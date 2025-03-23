namespace FluentPort.SDK;

public class LoginRequest
{
    public string? Token { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }

    public LoginRequest()
    { }
    public LoginRequest(string token, string username, string email, string passwordHash)
    {
        Token = token;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
    }
}
