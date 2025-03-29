namespace FluentPort.SDK;

public class RequestPasswordResetRequest
{
    public string? Email { get; set; }

    public RequestPasswordResetRequest()
    { }
    public RequestPasswordResetRequest(string email)
    {
        Email = email;
    }
}
