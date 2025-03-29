namespace FluentPort.SDK;

public class SendFeedbackRequest
{
    public LoginRequest? Request { get; set; }
    public string? Text { get; set; }

    public SendFeedbackRequest()
    { }
    public SendFeedbackRequest(LoginRequest request, string text)
    {
        Request = request;
        Text = text;
    }
}
