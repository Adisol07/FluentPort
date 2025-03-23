namespace FluentPort.SDK;

public class AccountInfo
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public byte[]? ProfilePicture { get; set; }
    public List<TunnelInfo>? Tunnels { get; set; }

    public AccountInfo()
    { }
    public AccountInfo(string username, string email, string role, byte[] profilePicture, List<TunnelInfo> tunnels)
    {
        Username = username;
        Email = email;
        Role = role;
        ProfilePicture = profilePicture;
        Tunnels = tunnels;
    }
}
