namespace FluentPort.SDK;

public class ServerOperationRequest
{
    public string? Key { get; set; }
    public string? Target { get; set; }

    public ServerOperationRequest()
    { }
    public ServerOperationRequest(string key, string target)
    {
        Key = key;
        Target = target;
    }
}
