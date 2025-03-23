using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;

namespace FluentPort.SDK;

public class TunnelServer
{
    private TcpListener listener;

    public X509Certificate2 Certificate;
    public Dictionary<Guid, TunnelConnection> Connections;
    public Dictionary<Guid, DateTime> LastActivity;
    public TimeSpan Timeout = TimeSpan.FromSeconds(30);
    public Action<string, int>? ClientConnected;
    public Action<string, int>? ClientDisconnected;
    public Action? OnStart;
    public Action? OnStop;
    public Action? OnTunnelClientConnected;
    public Action? OnTunnelClientDisconnected;

    public TunnelServer(int listenPort, X509Certificate2 cert)
    {
        listener = new TcpListener(IPAddress.Any, listenPort);
        Connections = new Dictionary<Guid, TunnelConnection>();
        LastActivity = new Dictionary<Guid, DateTime>();
        Certificate = cert;
    }

    public void Start()
    {
        listener.Start();
        _ = listen();
    }
    public void Stop()
    {
        lock (Connections)
        {
            foreach (var c in Connections)
            {
                c.Value.CTS.Cancel();
                foreach (var c2 in c.Value.RemoteConnections)
                {
                    c2.Value.Close();
                    c2.Value.Dispose();
                }
                c.Value.TunnelClient.Close();
                c.Value.TunnelClient.Dispose();
                if (OnTunnelClientDisconnected != null)
                    OnTunnelClientDisconnected.Invoke();
            }
            Connections.Clear();
            LastActivity.Clear();
        }
        listener.Stop();
        if (OnStop != null)
            OnStop.Invoke();
    }

    private async Task listen()
    {
        try
        {
            if (OnStart != null)
                OnStart.Invoke();
            _ = clear_disconnected();
            while (listener.Server.IsBound)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Logger.Log(new Log(LogLevels.Information, "TunnelServer.listen", "Accepted tunnel client"));
                _ = handle_tunnel_connection(client);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.listen[" + ex.StackTrace + "]", ex.Message));
        }
    }

    private async Task handle_tunnel_connection(TcpClient tunnelClient)
    {
        Guid guid = Guid.NewGuid();
        try
        {
            TunnelConnection connection = new TunnelConnection(tunnelClient, 0);
            connection.Stream!.AuthenticateAsServer(Certificate, clientCertificateRequired: false, checkCertificateRevocation: true);
            await read_tunnel(connection, guid);
            if (string.IsNullOrEmpty(connection.UserToken!.ToString()))
            {
                tunnelClient.Close();
                return;
            }
            Logger.Log(new Log(LogLevels.Information, "TunnelServer.listen", "Forwarding " + guid.ToString() + " <-> " + connection.RemotePort));
            Connections.Add(guid, connection);
            if (OnTunnelClientConnected != null)
                OnTunnelClientConnected.Invoke();

            // Listen, share
            _ = listen_remote_clients(connection);
            _ = handle_shared_message(connection);

            // Read
            while (listener.Server.IsBound &&
                   connection.TunnelClient.Connected &&
                   Connections.ContainsKey(guid) &&
                   !connection.CTS.IsCancellationRequested)
            {
                await read_tunnel(connection, guid);
            }

            connection.CTS.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.handle_tunnel_connection[" + ex.StackTrace + "]", ex.Message));
        }
        lock (Connections)
        {
            if (Connections.ContainsKey(guid))
                Connections.Remove(guid);
        }
        if (OnTunnelClientDisconnected != null)
            OnTunnelClientDisconnected.Invoke();
    }

    private async Task read_tunnel(TunnelConnection connection, Guid connectionGuid)
    {
        try
        {
            TcpMessage tunnel_read = await TcpMessage.Read(connection.Stream!, connection.TunnelClient, connection.CTS.Token);
            if (tunnel_read == null)
                return;
            if (tunnel_read.ProtocolVersion != Utils.Version)
            {
                Logger.Log(new Log(LogLevels.Error, "TunnelServer.read_tunnel", "Imcompatible protocol version! (" + Utils.Version + " != " + tunnel_read.ProtocolVersion + ")"));
                return;
            }

            foreach (Result result in tunnel_read.Results!)
            {
                switch (result.Type)
                {
                    case ResultTypes.Packet:
                        Packet packet = JsonSerializer.Deserialize<Packet>(result.Data!.ToString()!)!;
                        Logger.Log(new Log(LogLevels.Information, "TunnelServer.read_tunnel", "Received packet from " + packet.Origin));
                        if (listener.Server.IsBound &&
                            connection.TunnelClient.Connected &&
                            packet.Bytes!.Length > 0 &&
                            connection.RemoteConnections.TryGetValue(packet.Origin, out TcpClient? c))
                        {
                            if (!c.Connected)
                            {
                                Logger.Log(new Log(LogLevels.Warning, "TunnelServer.read_tunnel", "Packet origin is not connected on the remote end (" + packet.Origin + ")"));
                                continue;
                            }

                            await c.GetStream().WriteAsync(packet.Bytes, 0, packet.Bytes.Length);
                        }
                        else
                        {
                            Logger.Log(new Log(LogLevels.Warning, "TunnelServer.read_tunnel", "Packet was malformed or the connection is not active"));
                        }
                        break;
                    case ResultTypes.ClientDisconnected:
                        Logger.Log(new Log(LogLevels.Information, "TunnelServer.read_tunnel", "Disconnected client: " + result.Data!.ToString()));
                        Guid disconnected_guid = Guid.Parse(result.Data.ToString()!);
                        disconnect_client(connection, disconnected_guid, false);
                        break;
                    case ResultTypes.Authorization:
                        string authorization_string = result.Data!.ToString()!;
                        string user_token = authorization_string.Split(';')[0];
                        Guid tunnel_id = Guid.Parse(authorization_string.Split(';')[1]);
                        Result authorization_result = connection.Authorize(user_token, tunnel_id);
                        lock (connection.SharedMessage)
                        {
                            connection.SharedMessage.Results!.Add(authorization_result);
                        }
                        break;
                    default:
                        Logger.Log(new Log(LogLevels.Warning, "TunnelServer.read_tunnel", "Invalid Result type! (" + result.Type.ToString() + ")"));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.read_tunnel[" + ex.StackTrace + "]", ex.Message));
        }
    }

    private async Task handle_remote_client(TunnelConnection connection, Guid remoteGuid, TcpClient remoteClient)
    {
        try
        {
            Result connect_result = new Result(ResultTypes.ClientConnected, remoteGuid.ToString());
            lock (connection.SharedMessage)
            {
                connection.SharedMessage.Results!.Add(connect_result);
            }
            connection.RemoteConnections.Add(remoteGuid, remoteClient);
            LastActivity.Add(remoteGuid, DateTime.Now);
            Logger.Log(new Log(LogLevels.Information, "TunnelServer.handle_remote_client", "Handling remote: " + remoteGuid));

            while (listener.Server.IsBound &&
                   connection.RemoteListener.Server.IsBound &&
                   remoteClient.Connected &&
                   !connection.CTS.IsCancellationRequested)
            {
                if (remoteClient.Available == 0)
                {
                    await Task.Delay(10);
                    continue;
                }

                byte[] bytes = await Utils.ReadAsync(remoteClient.GetStream(), connection.CTS);
                if (connection.CTS.IsCancellationRequested)
                    break;
                LastActivity[remoteGuid] = DateTime.Now;
                Packet packet = new Packet(remoteGuid, bytes);
                Result result = new Result(ResultTypes.Packet, packet);
                lock (connection.SharedMessage)
                {
                    connection.SharedMessage.Results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.handle_remote_client[" + ex.StackTrace + "]", ex.Message));
        }
        disconnect_client(connection, remoteGuid);
    }

    private async Task listen_remote_clients(TunnelConnection connection)
    {
        try
        {
            while (listener.Server.IsBound &&
                   connection.TunnelClient.Connected &&
                   !connection.CTS.IsCancellationRequested)
            {
                TcpClient remote_client = await connection.RemoteListener.AcceptTcpClientAsync(connection.CTS.Token);
                if (connection.CTS.IsCancellationRequested)
                    return;
                if (ClientConnected != null)
                {
                    IPEndPoint ip = (IPEndPoint)remote_client.Client.RemoteEndPoint!;
                    ClientConnected.Invoke(ip.Address.ToString(), ip.Port);
                }
                Guid guid = Guid.NewGuid();
                _ = handle_remote_client(connection, guid, remote_client);
            }
            connection.CTS.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.listen_remote_clients[" + ex.StackTrace + "]", ex.Message));
        }
    }

    private void disconnect_client(TunnelConnection connection, Guid remoteGuid, bool notifyClient = true)
    {
        try
        {
            lock (connection)
            {
                if (connection.RemoteConnections.TryGetValue(remoteGuid, out TcpClient? c))
                {
                    if (ClientDisconnected != null)
                    {
                        IPEndPoint ip = (IPEndPoint)c.Client.RemoteEndPoint!;
                        ClientDisconnected.Invoke(ip.Address.ToString(), ip.Port);
                    }
                    if (c.Connected)
                    {
                        c.Close();
                    }
                    connection.RemoteConnections.Remove(remoteGuid);
                    c.Dispose();
                }
                if (LastActivity.ContainsKey(remoteGuid))
                    LastActivity.Remove(remoteGuid);
                if (notifyClient)
                {
                    Result notify = new Result(ResultTypes.ClientDisconnected, remoteGuid.ToString());
                    lock (connection.SharedMessage)
                    {
                        connection.SharedMessage.Results!.Add(notify);
                    }
                }
                Logger.Log(new Log(LogLevels.Information, "TunnelServer.disconnect_client", "Attempted to disconnect: " + remoteGuid));
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.disconnect_client[" + ex.StackTrace + "]", ex.Message));
        }
    }

    private async Task handle_shared_message(TunnelConnection connection)
    {
        try
        {
            while (listener.Server.IsBound &&
                   connection.TunnelClient.Connected &&
                   !connection.CTS.IsCancellationRequested)
            {
                lock (connection.SharedMessage)
                {
                    if (connection.SharedMessage.Results!.Count > 0)
                    {
                        Logger.Log(new Log(LogLevels.Information, "TunnelServer.handle_shared_message", "Handling shared message (" + connection.SharedMessage.Results.Count + ")[" + string.Join(", ", connection.SharedMessage.Results.Select((x) => { return x.Type.ToString(); })) + "]"));
                        Utils.WriteMessage(connection.Stream!, connection.TunnelClient, connection.SharedMessage).Wait();
                    }
                    connection.SharedMessage.Results!.Clear();
                }
                await Task.Delay(10);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.handle_shared_message[" + ex.StackTrace + "]", ex.Message));
        }
    }

    private async Task clear_disconnected()
    {
        try
        {
            while (listener.Server.IsBound)
            {
                foreach (TunnelConnection connection in Connections.Values)
                {
                    List<Guid> check = new List<Guid>();
                    foreach (var c in LastActivity)
                    {
                        if (DateTime.Now - c.Value > Timeout)
                        {
                            check.Add(c.Key);
                        }
                    }
                    foreach (Guid guid in check)
                    {
                        if (!Utils.IsClientConnected(connection.RemoteConnections[guid]))
                        {
                            Logger.Log(new Log(LogLevels.Information, "TunnelServer.clear_disconnected", "Clearing: " + guid));
                            disconnect_client(connection, guid);
                        }
                    }
                    check.Clear();

                    if (!connection.TunnelClient.Connected || !Utils.IsClientConnected(connection.TunnelClient))
                    {
                        Logger.Log(new Log(LogLevels.Information, "TunnelServer.clear_disconnected", "Clearing tunnel connection"));
                        lock (connection)
                        {
                            connection.CTS.Cancel();
                            connection.TunnelClient.Close();
                            List<Guid> d = new List<Guid>();
                            foreach (var r in connection.RemoteConnections)
                                d.Add(r.Key);
                            foreach (Guid g in d)
                                disconnect_client(connection, g);
                        }
                    }
                }

                await Task.Delay((int)Timeout.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelServer.clear_disconnected[" + ex.StackTrace + "]", ex.Message));
        }
    }
}
