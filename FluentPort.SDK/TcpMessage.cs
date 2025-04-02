using System.Text.Json;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;

namespace FluentPort.SDK;

public class TcpMessage
{
    public string ProtocolVersion => "0.1.2";
    public List<Result>? Results { get; set; }

    public TcpMessage()
    {
        Results = new List<Result>();
    }
    public TcpMessage(List<Result> results)
    {
        Results = results;
    }

    public byte[] Construct()
    {
        byte[] message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
        byte[] length = BitConverter.GetBytes(message.Length);
        return [.. length, .. message];
    }
    public static async Task<TcpMessage> Read(SslStream stream, TcpClient client, CancellationToken ct)
    {
        try
        {
            if (client.Client.IsBound)
            {
                byte[] size_buffer = new byte[4];
                int bytes_read = await stream.ReadAsync(size_buffer, 0, 4, ct);
                if (ct.IsCancellationRequested)
                    return null!;

                if (bytes_read != 4)
                    return null!;

                int message_size = BitConverter.ToInt32(size_buffer, 0);
                byte[] message_buffer = new byte[message_size];
                int total_bytes_read = 0;

                while (total_bytes_read < message_size)
                {
                    int remaining_bytes = message_size - total_bytes_read;
                    int bytes_read_this_time = await stream.ReadAsync(message_buffer, total_bytes_read, remaining_bytes, ct);
                    if (ct.IsCancellationRequested)
                        return null!;

                    if (bytes_read_this_time == 0)
                        break;

                    total_bytes_read += bytes_read_this_time;
                }

                if (total_bytes_read == message_size && !ct.IsCancellationRequested)
                {
                    string json_string = Encoding.UTF8.GetString(message_buffer);
                    TcpMessage msg = JsonSerializer.Deserialize<TcpMessage>(json_string)!;
                    return msg;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(new Log(LogLevels.Error, "TcpMessage.Read", ex.Message));
        }
        return null!;
    }
}
