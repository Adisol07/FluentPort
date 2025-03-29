using System.Net.Sockets;
using System.Net.Security;

namespace FluentPort.SDK;

public static class Utils
{
    public static string Version => "0.1.1";
#if DEBUG
    public const string API_SERVER = "http://localhost:5055";
#else
    public const string API_SERVER = "https://api.fluentport.com";
#endif

    public static bool IsClientConnected(TcpClient client)
    {
        try
        {
            if (!client.Connected)
                return false;

            Socket socket = client.Client;
            bool poll = socket.Poll(1000, SelectMode.SelectRead);
            bool available = (socket.Available == 0);

            return !(poll && available);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<byte[]> ReadAsync(NetworkStream stream, CancellationTokenSource cts)
    {
        byte[] buffer = new byte[4096];
        try
        {
            int bytes_read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            if (bytes_read > 0 && !cts.IsCancellationRequested)
            {
                byte[] result = new byte[bytes_read];
                Array.Copy(buffer, result, bytes_read);
                return result;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Utils.ReadAsync", ex.Message));
        }
        return Array.Empty<byte>();
    }

    public static async Task WriteMessage(SslStream stream, TcpClient client, TcpMessage message)
    {
        try
        {
            byte[] bytes = message.Construct();
            if (client.Client.IsBound)
            {
                Logger.Log(new Log(LogLevels.Information, "Utils.WriteMessage", "Writing message"));
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "Utils.WriteMessage", ex.Message));
        }
    }
}
