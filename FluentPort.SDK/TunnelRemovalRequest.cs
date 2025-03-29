namespace FluentPort.SDK;

public class TunnelRemovalRequest
{
    public LoginRequest? Request { get; set; }
    public string? ID { get; set; }

    public TunnelRemovalRequest()
    { }
    public TunnelRemovalRequest(LoginRequest request, string id)
    {
        Request = request;
        ID = id;
    }
}
