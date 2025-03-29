using FluentPort.SDK;

namespace DemoConsole1;

class Program
{
    static void Main()
    {
        Logger.OnLog += (log) =>
        {
            Console.WriteLine(log);
        };
        TunnelClient client = new TunnelClient(Guid.NewGuid(), Guid.NewGuid());
        client.Connect("167.71.35.0", 33, "127.0.0.1", 25565);
        Console.ReadKey();
        client.Disconnect();
    }
}
