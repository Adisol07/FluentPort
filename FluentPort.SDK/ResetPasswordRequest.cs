namespace FluentPort.SDK;

public class ResetPasswordCodeRequest
{
    public string? Code { get; set; }

    public ResetPasswordCodeRequest()
    { }
    public ResetPasswordCodeRequest(string code)
    {
        Code = code;
    }
}
