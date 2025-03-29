namespace FluentPort.SDK;

public class CheckPasswordResetCodeRequest
{
    public string? Code { get; set; }

    public CheckPasswordResetCodeRequest()
    { }
    public CheckPasswordResetCodeRequest(string code)
    {
        Code = code;
    }
}
