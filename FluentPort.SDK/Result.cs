namespace FluentPort.SDK;

public class Result
{
    public ResultTypes Type { get; set; }
    public object? Data { get; set; }

    public Result()
    { }
    public Result(ResultTypes type, object data)
    {
        Type = type;
        Data = data;
    }
}
public enum ResultTypes
{
    Unknown = 0,
    Packet = 1,
    ClientConnected = 2,
    ClientDisconnected = 3,
    PortAssignment = 4,
    Authorization = 5,
    Error = 6,
}
