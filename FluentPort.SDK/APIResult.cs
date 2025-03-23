namespace FluentPort.SDK;

public class APIResult
{
    public string? Message { get; set; }
    public APIResultStatus Status { get; set; }
    public object? Tag { get; set; }

    public APIResult()
    { }
    public APIResult(string message, APIResultStatus status, object tag = null!)
    {
        Message = message;
        Status = status;
        Tag = tag;
    }
}
public enum APIResultStatus
{
    Unknown = 0,
    Success = 1,
    Failed = 2,
}
