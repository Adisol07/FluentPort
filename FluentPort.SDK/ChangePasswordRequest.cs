namespace FluentPort.SDK;

public class ChangePasswordRequest
{
    public LoginRequest? Request { get; set; }
    public string? NewPasswordHash { get; set; }

    public ChangePasswordRequest()
    { }
    public ChangePasswordRequest(LoginRequest request, string newPasswordHash)
    {
        Request = request;
        NewPasswordHash = newPasswordHash;
    }
}
