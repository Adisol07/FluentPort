using FluentPort.SDK;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace FluentPort;

public static class UserInfo
{
    public static string? Token { get; set; }
    public static string? Username { get; set; }
    public static string? Email { get; set; }
    public static string? Role { get; set; }
    public static byte[]? ProfilePicture { get; set; }
    public static List<TunnelInfo>? Tunnels { get; set; }

    public static async Task<bool> Get()
    {
        if (string.IsNullOrEmpty(Token))
            return false;
        APIResult? api_result = await User.GetInfo(new LoginRequest(Token, "", "", ""));
        if (api_result?.Message == "Success")
        {
            AccountInfo info = JsonSerializer.Deserialize<AccountInfo>(api_result.Tag?.ToString()!)!;

            Username = info.Username;
            Email = info.Email;
            Role = info.Role;
            ProfilePicture = info.ProfilePicture;
            Tunnels = info.Tunnels;

            return true;
        }
        return false;
    }
    public static async Task<APIResult?> Set()
    {
        if (string.IsNullOrEmpty(Token))
            return new APIResult("Failed", APIResultStatus.Failed, "Invalid token");
        return await User.SetInfo(new SetAccountInfoRequest(new LoginRequest(Token, "", "", ""), new AccountInfo(Username!, Email!, Role!, ProfilePicture!, Tunnels!)));
    }
    public static async Task<APIResult?> DeleteAccount()
    {
        if (string.IsNullOrEmpty(Token))
            return new APIResult("Failed", APIResultStatus.Failed, "Invalid token");
        return await User.DeleteAccount(new LoginRequest(Token, "", "", ""));
    }
}
