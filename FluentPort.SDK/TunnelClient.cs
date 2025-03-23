using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace FluentPort.SDK;

public class TunnelClient
{
    private TcpClient client;
    private SslStream? stream;
    private TcpMessage shared_message;
    private CancellationTokenSource cts;
    private string? tunnel_host;
    private DateTime message_read_measurement;

    public string UserToken;
    public Guid TunnelID;
    public Dictionary<Guid, TcpClient> Connections;
    public Dictionary<Guid, DateTime> LastActivity;
    public string LocalHost = "127.0.0.1";
    public int LocalPort = 80;
    public int RemotePort = 0;
    public TimeSpan Timeout = TimeSpan.FromSeconds(30);
    public int Ping;
    public Action<string, int>? ClientConnected;
    public Action<string, int>? ClientDisconnected;
    public Action? OnStart;
    public Action? OnStop;
    public Action<string>? OnClientConnected;
    public Action<string>? OnClientDisconnected;
    public Action<Packet>? OnPacket;

    public TunnelClient(string userToken, Guid tunnelID)
    {
        UserToken = userToken;
        TunnelID = tunnelID;
        client = new TcpClient();
        Connections = new Dictionary<Guid, TcpClient>();
        LastActivity = new Dictionary<Guid, DateTime>();
        shared_message = new TcpMessage();
        cts = new CancellationTokenSource();
    }

    public void Connect(string tunnelHost, int tunnelPort, string localHost, int localPort)
    {
        Connections.Clear();
        client.Connect(tunnelHost, tunnelPort);
        LocalHost = localHost;
        LocalPort = localPort;
        tunnel_host = tunnelHost;
        _ = listen();
    }
    public void Disconnect()
    {
        cts.Cancel();
        client.Close();
        foreach (var c in Connections)
        {
            c.Value.Close();
            c.Value.Dispose();
        }
        client.Dispose();
        Connections.Clear();
    }

    private async Task listen()
    {
        try
        {
            stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(val_server_cert!));
            stream.AuthenticateAsClient(tunnel_host!);

            lock (shared_message)
            {
                shared_message.Results!.Add(new Result(ResultTypes.Authorization, UserToken + ";" + TunnelID.ToString()));
            }

            // Share and clear
            _ = handle_shared_message();
            _ = clear_disconnected();

            // Read
            while (client.Connected && !cts.IsCancellationRequested)
            {
                await read_tunnel();
            }

            if (OnStop != null)
                OnStop.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.listen", ex.Message));
        }
    }

    private async Task read_tunnel()
    {
        try
        {
            message_read_measurement = DateTime.Now;
            TcpMessage read_message = await TcpMessage.Read(stream!, client, cts.Token);
            Ping = (int)(DateTime.Now - message_read_measurement).TotalMilliseconds;
            if (read_message.ProtocolVersion != Utils.Version)
            {
                Logger.Log(new Log(LogLevels.Error, "TunnelClient.read_tunnel", "Imcompatible protocol version! (" + Utils.Version + " != " + read_message.ProtocolVersion + ")"));
                return;
            }

            foreach (Result result in read_message.Results!)
            {
                switch (result.Type)
                {
                    case ResultTypes.Packet:
                        Packet packet = JsonSerializer.Deserialize<Packet>(result.Data!.ToString()!)!;
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.read_tunnel", "Received packet from: " + packet.Origin.ToString()));
                        if (packet != null &&
                            packet.Bytes!.Length > 0 &&
                            Connections.TryGetValue(packet.Origin, out TcpClient? c))
                        {
                            if (!c.Connected)
                            {
                                Logger.Log(new Log(LogLevels.Warning, "TunnelClient.read_tunnel", "Packet origin is not connected on the local end (" + packet.Origin + ")"));
                                continue;
                            }
                            if (OnPacket != null)
                                OnPacket.Invoke(packet);

                            await c.GetStream().WriteAsync(packet.Bytes, 0, packet.Bytes.Length);
                        }
                        break;
                    case ResultTypes.ClientConnected:
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.read_tunnel", "Connected client: " + result.Data!.ToString()));
                        TcpClient connected = new TcpClient();
                        Guid connected_guid = Guid.Parse(result.Data!.ToString()!);
                        if (OnClientConnected != null)
                            OnClientConnected.Invoke(connected_guid.ToString());
                        connected.Connect("127.0.0.1", LocalPort);
                        // Listen
                        _ = Task.Run(() => handle_local_client(connected_guid, connected));
                        break;
                    case ResultTypes.ClientDisconnected:
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.read_tunnel", "Disconnected client: " + result.Data!.ToString()));
                        Guid disconnected_guid = Guid.Parse(result.Data!.ToString()!);
                        if (OnClientDisconnected != null)
                            OnClientDisconnected.Invoke(disconnected_guid.ToString());
                        disconnect_client(disconnected_guid, false);
                        break;
                    case ResultTypes.PortAssignment:
                        RemotePort = Convert.ToInt32(result.Data!.ToString());
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.read_tunnel", "Port was successfully assigned to: " + result.Data!.ToString()));
                        if (OnStart != null)
                            OnStart.Invoke();
                        break;
                    default:
                        Logger.Log(new Log(LogLevels.Warning, "TunnelClient.read_tunnel", "Invalid Result type! (" + result.Type.ToString() + ")"));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.read_tunnel", ex.Message));
        }
    }

    private async Task handle_local_client(Guid localGuid, TcpClient localClient)
    {
        try
        {
            Connections.Add(localGuid, localClient);
            if (ClientConnected != null)
            {
                IPEndPoint ip = (IPEndPoint)localClient.Client.RemoteEndPoint!;
                ClientConnected.Invoke(ip.Address.ToString(), ip.Port);
            }
            Logger.Log(new Log(LogLevels.Information, "TunnelClient.handle_local_client", "Handling local client: " + localGuid));

            while (client.Connected &&
                   localClient.Connected &&
                   !cts.IsCancellationRequested)
            {
                if (localClient.Available == 0)
                {
                    await Task.Delay(10);
                    continue;
                }

                byte[] bytes = await Utils.ReadAsync(localClient.GetStream(), cts);
                if (cts.IsCancellationRequested)
                    break;
                LastActivity[localGuid] = DateTime.Now;
                Packet packet = new Packet(localGuid, bytes);
                Result result = new Result(ResultTypes.Packet, packet);
                lock (shared_message)
                {
                    shared_message.Results!.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.handle_local_client", ex.Message));
        }
        disconnect_client(localGuid, false);
    }

    private async Task handle_shared_message()
    {
        try
        {
            while (client.Connected && !cts.IsCancellationRequested)
            {
                lock (shared_message)
                {
                    if (shared_message.Results!.Count > 0)
                    {
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.handle_shared_message", "Handling shared message (" + shared_message.Results.Count + ")[" + string.Join(", ", shared_message.Results.Select((x) => { return x.Type.ToString(); })) + "]"));
                        Utils.WriteMessage(stream!, client, shared_message).Wait();
                    }
                    shared_message.Results!.Clear();
                }
                await Task.Delay(10);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.handle_shared_message", ex.Message));
        }
    }

    private async Task clear_disconnected()
    {
        try
        {
            while (client.Connected)
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
                    if (!Utils.IsClientConnected(Connections[guid]))
                    {
                        Logger.Log(new Log(LogLevels.Information, "TunnelClient.clear_disconnected", "Clearing: " + guid));
                        disconnect_client(guid);
                    }
                }

                await Task.Delay((int)Timeout.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.clear_disconnected", ex.Message));
        }
    }

    private void disconnect_client(Guid guid, bool notifyServer = true)
    {
        try
        {
            if (notifyServer)
            {
                Result notify = new Result(ResultTypes.ClientDisconnected, guid);
                lock (shared_message)
                {
                    shared_message.Results!.Add(notify);
                }
            }
            if (LastActivity.ContainsKey(guid))
                LastActivity.Remove(guid);
            if (!Connections.ContainsKey(guid))
                return;
            TcpClient c = Connections[guid];
            if (ClientDisconnected != null)
            {
                IPEndPoint ip = (IPEndPoint)c.Client.RemoteEndPoint!;
                ClientDisconnected.Invoke(ip.Address.ToString(), ip.Port);
            }
            c.Close();
            c.Dispose();
            Connections.Remove(guid);
            Logger.Log(new Log(LogLevels.Information, "TunnelClient.disconnect_client", "Attempted to disconnect client: " + guid));
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelClient.disconnect_client", ex.Message));
        }
    }

    private static bool val_server_cert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}
