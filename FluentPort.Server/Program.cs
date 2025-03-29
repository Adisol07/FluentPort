using FluentPort.SDK;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;

namespace FluentPort.Server;

class Program
{
    static string app_path => Environment.CurrentDirectory;

    static async Task Main()
    {
        if (!Directory.Exists(app_path + "/logs"))
            Directory.CreateDirectory(app_path + "/logs");

        Logger.OnLog += (log) =>
        {
            if (log.Level != LogLevels.Information)
                Console.WriteLine(log);
        };

        string target = File.ReadAllText(app_path + "/target.txt").Trim();
        string key = File.ReadAllText(app_path + "/key.txt").Trim();
        string cert_pwd = File.ReadAllText(app_path + "/server-certificate-pwd.txt").Trim();
        X509Certificate2 server_cert = new X509Certificate2(app_path + "/server-certificate.pfx", cert_pwd);
        TunnelServer server = new TunnelServer(33, server_cert);
        server.OnStart += () =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Started server (" + target + ")");
            Console.ResetColor();
        };
        server.OnStop += () =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stopped server");
            Console.ResetColor();
        };
        server.OnTunnelClientConnected += async () =>
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
                string json = JsonSerializer.Serialize(new ServerOperationRequest(key, target));
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/forwarding/tunnel_connected_on_server", content);
                if (response.IsSuccessStatusCode)
                {
                    APIResult? result = await response.Content.ReadFromJsonAsync<APIResult>();
                    Logger.Log(new Log(LogLevels.Information, "OnTunnelClientConnected", result!.Tag!.ToString()!));
                }
                else
                {
                    Logger.Log(new Log(LogLevels.Error, "OnTunnelClientConnected", response.StatusCode.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new Log(LogLevels.Error, "OnTunnelClientConnected", ex.Message));
            }
        };
        server.OnTunnelClientDisconnected += async () =>
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
                string json = JsonSerializer.Serialize(new ServerOperationRequest(key, target));
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(Utils.API_SERVER + "/forwarding/tunnel_disconnected_from_server", content);
                if (response.IsSuccessStatusCode)
                {
                    APIResult? result = await response.Content.ReadFromJsonAsync<APIResult>();
                    Logger.Log(new Log(LogLevels.Information, "OnTunnelClientDisconnected", result!.Tag!.ToString()!));
                }
                else
                {
                    Logger.Log(new Log(LogLevels.Error, "OnTunnelClientDisconnected", response.StatusCode.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new Log(LogLevels.Error, "OnTunnelClientDisconnected", ex.Message));
            }
        };
        server.Start();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            server.Stop();
            Environment.Exit(0);
        };

        while (true)
        {
            // Save logs
            if (Logger.Logs.Count > 0)
                Logger.Save(app_path + "/logs/" + DateTime.Now.Ticks + ".json", true);

            await Task.Delay(TimeSpan.FromMinutes(2));
        }
    }
}
