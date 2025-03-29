using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace FluentPort.SDK;

public class Tunnel
{
    private TunnelClient? client;

    public string? UserToken { get; set; }
    public TunnelInfo? Info { get; set; }
    public string? ServerTarget { get; set; }
    public bool IsOpen { get; set; }
    public int Ping => client!.Ping;

    public Action? OnStart;
    public Action? OnStop;
    public Action<string>? ClientConnected;
    public Action<string>? ClientDisconnected;
    public Action<Packet>? PacketReceived;

    public Tunnel()
    { }
    public Tunnel(TunnelInfo info, string userToken)
    {
        IsOpen = false;
        UserToken = userToken;
        Info = info;
        create_client(userToken, info);
    }
    private void create_client(string userToken, TunnelInfo info)
    {
        client = new TunnelClient(userToken, Guid.Parse(info.ID!));
        client.OnStart += () =>
        {
            IsOpen = true;
            Info!.RemotePort = client.RemotePort;
            if (OnStart != null)
                OnStart.Invoke();
        };
        client.OnStop += () =>
        {
            IsOpen = false;
            if (OnStop != null)
                OnStop.Invoke();
            create_client(userToken, info);
        };
        client.OnClientConnected += (guid) =>
        {
            if (ClientConnected != null)
                ClientConnected.Invoke(guid);
        };
        client.OnClientDisconnected += (guid) =>
        {
            if (ClientDisconnected != null)
                ClientDisconnected.Invoke(guid);
        };
        client.OnPacket += (packet) =>
        {
            if (PacketReceived != null)
                PacketReceived.Invoke(packet);
        };
    }

    public async Task<APIResult?> Open()
    {
        if (string.IsNullOrEmpty(Info?.RemoteAddress))
        {
            APIResult? r = await assign_server();
            if (r?.Status != APIResultStatus.Success)
                return r;
            Server server = JsonSerializer.Deserialize<Server>(r.Tag!.ToString()!)!;
            Info!.RemoteAddress = server.Address;
            ServerTarget = server.Target;
        }
        if (client == null || IsOpen)
            return null;

        try
        {
            client?.Connect(Info?.RemoteAddress!, 33, Info?.LocalAddress!, Info!.LocalPort);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Tunnel.Open", ex.Message));
            return null;
        }
    }

    public void Close()
    {
        if (client == null || !IsOpen)
            return;

        try
        {
            client?.Disconnect();
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Tunnel.Close", ex.Message));
        }
    }

    public async Task<APIResult?> SaveTunnel()
    {
        APIResult? r = await save_tunnel();
        Info!.ID = r!.Tag!.ToString();
        return r;
    }

    public async Task<APIResult?> RemoveTunnel()
    {
        return await remove_tunnel();
    }

    private async Task<APIResult?> save_tunnel()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(new TunnelSaveRequest(
                        new LoginRequest(UserToken!, "", "", "")!,
                        Info!));
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/forwarding/save_port", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    private async Task<APIResult?> remove_tunnel()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            string json = JsonSerializer.Serialize(new TunnelRemovalRequest(new LoginRequest(UserToken!, "", "", "")!, Info?.ID!));
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/forwarding/remove_port", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
    private async Task<APIResult?> assign_server()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            HttpResponseMessage response = await client.GetAsync(Utils.API_SERVER + "/forwarding/assign_server");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<APIResult>();
            }
            else
            {
                return new APIResult() { Message = response.StatusCode.ToString() };
            }
        }
        catch (Exception ex)
        {
            return new APIResult() { Message = ex.Message };
        }
    }
}
