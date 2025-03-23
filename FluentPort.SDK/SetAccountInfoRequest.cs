namespace FluentPort.SDK;

public class SetAccountInfoRequest
{
    public LoginRequest? Request { get; set; }
    public AccountInfo? Info { get; set; }

    public SetAccountInfoRequest()
    { }
    public SetAccountInfoRequest(LoginRequest request, AccountInfo info)
    {
        Request = request;
        Info = info;
    }
}
