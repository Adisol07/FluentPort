namespace FluentPort.SDK;

public class Packet
{
    public Guid Origin { get; set; }
    public byte[]? Bytes { get; set; }

    public Packet()
    { }
    public Packet(Guid origin, byte[] bytes)
    {
        Origin = origin;
        Bytes = bytes;
    }
}
