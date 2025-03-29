namespace FluentPort.SDK;

public class TunnelInfo
{
    public string? ID { get; set; }
    public string? LocalAddress { get; set; }
    public string? RemoteAddress { get; set; }
    public int LocalPort { get; set; }
    public int RemotePort { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public TunnelInfo()
    { }
    public TunnelInfo(string id, string localAddress, string remoteAddress, int localPort, int remotePort, DateTimeOffset createdAt)
    {
        ID = id;
        LocalAddress = localAddress;
        RemoteAddress = remoteAddress;
        LocalPort = localPort;
        RemotePort = remotePort;
        CreatedAt = createdAt;
    }
}
