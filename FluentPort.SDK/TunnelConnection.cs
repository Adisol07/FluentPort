using System.Net;
using System.Net.Sockets;
using System.Net.Security;

namespace FluentPort.SDK;

public class TunnelConnection
{
    public string? UserToken { get; set; }
    public TcpMessage SharedMessage { get; set; }
    public TcpClient TunnelClient { get; set; }
    public TcpListener RemoteListener { get; set; }
    public int RemotePort { get; set; }
    public Dictionary<Guid, TcpClient> RemoteConnections { get; set; }
    public CancellationTokenSource CTS { get; set; }
    public SslStream? Stream { get; set; }

    public TunnelConnection(TcpClient tunnelClient, int remotePort)
    {
        SharedMessage = new TcpMessage();
        TunnelClient = tunnelClient;
        RemotePort = remotePort;
        RemoteListener = new TcpListener(IPAddress.Any, remotePort);
        RemoteConnections = new Dictionary<Guid, TcpClient>();
        CTS = new CancellationTokenSource();
        Stream = new SslStream(tunnelClient.GetStream(), false);
    }

    public Result Authorize(string userToken, Guid tunnelID)
    {
        try
        {
            // APIResult api_result = await assign(userToken, tunnelID);
            // if (api_result.Status != APIResultStatus.Success)
            // {
            //     return new Result(ResultTypes.Error, "Error: API request failed!");
            // }

            UserToken = userToken;

            RemoteListener.Start();
            RemotePort = ((IPEndPoint)RemoteListener.LocalEndpoint).Port;

            Result result = new Result(ResultTypes.PortAssignment, RemotePort);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TunnelConnection.Authorize", ex.Message));
            return new Result(ResultTypes.Error, "Error: " + ex.Message);
        }
    }

    // private async Task<APIResult> assign(string userToken, Guid tunnelID)
    // {
    //     try
    //     {
    //         using HttpClient client = new HttpClient();
    //         client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
    //         string json = JsonSerializer.Serialize(new TunnelAssignmentRequest(userToken, tunnelID));
    //         HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
    //         HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/forwarding/assign", content);
    //         if (response.IsSuccessStatusCode)
    //         {
    //             return (await response.Content.ReadFromJsonAsync<APIResult>())!;
    //         }
    //         else
    //         {
    //             return new APIResult() { Message = response.StatusCode.ToString(), Status = APIResultStatus.Failed };
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         return new APIResult() { Message = ex.Message, Status = APIResultStatus.Failed };
    //     }
    // }
}
