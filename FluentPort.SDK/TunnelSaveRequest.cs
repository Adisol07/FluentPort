namespace FluentPort.SDK;

public class TunnelSaveRequest
{
    public LoginRequest? Request { get; set; }
    public TunnelInfo? Info { get; set; }

    public TunnelSaveRequest()
    { }
    public TunnelSaveRequest(LoginRequest request, TunnelInfo info)
    {
        Request = request;
        Info = info;
    }
}
