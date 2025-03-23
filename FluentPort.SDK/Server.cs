namespace FluentPort.SDK;

public class Server
{
    public string? Key { get; set; }
    public string? Address { get; set; }
    public string? Target { get; set; }
    public int MaxTunnels { get; set; }
    public int ActiveTunnels { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Server()
    { }
    public Server(string key, string address, string target, int maxTunnels, int activeTunnels, double latitude, double longtitude)
    {
        Key = key;
        Address = address;
        Target = target;
        Latitude = latitude;
        Longitude = longtitude;
        MaxTunnels = maxTunnels;
        ActiveTunnels = activeTunnels;
    }
}
