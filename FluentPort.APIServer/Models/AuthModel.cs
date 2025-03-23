using FluentPort.SDK;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace FluentPort.APIServer.Models;

public static class AuthModel
{
    public static string FeedbacksDir = "";
    public static Dictionary<string, string> TokenToID { get; set; } = new Dictionary<string, string>();
    public static Dictionary<string, string> EmailToID { get; set; } = new Dictionary<string, string>();
    public static Dictionary<string, string> UsernameToID { get; set; } = new Dictionary<string, string>();

    public static void Load(string userDir)
    {
        TokenToID.Clear();
        EmailToID.Clear();
        UsernameToID.Clear();
        string[] dirs = Directory.GetDirectories(userDir);
        foreach (string dir in dirs)
        {
            string id = Path.GetFileName(dir);
            Account account = Account.Load(id);
            if (!TokenToID.ContainsKey(account.Token!))
                TokenToID.Add(account.Token!, id);
            if (!EmailToID.ContainsKey(account.Email!))
                EmailToID.Add(account.Email!, id);
            if (!UsernameToID.ContainsKey(account.Username!))
                UsernameToID.Add(account.Username!, id);
            Logger.Log(new Log(LogLevels.Information, "Auth.Load", "Loaded account: " + id));
        }
    }

    public static void SaveFeedback(string userId, string feedback)
    {
        string json = JsonSerializer.Serialize(new FeedbackRecord(userId, feedback));
        File.WriteAllText(FeedbacksDir + "/" + DateTime.Now.Ticks + ".json", json);
        Logger.Log(new Log(LogLevels.Information, "Auth.SaveFeedback", "Saved feedback from: " + userId));
    }

    public static bool ValidUsername(string username)
    {
        string pattern = @"^[a-zA-Z0-9_.-]+$";
        return Regex.IsMatch(username, pattern);
    }
    public static bool ValidEmail(string email)
    {
        return email.Contains(".") && email.Contains("@") && !email.Contains(" ");
    }

    public static string GenerateToken()
    {
        byte[] token_bytes = new byte[32];
        RandomNumberGenerator.Fill(token_bytes);
        return Convert.ToBase64String(token_bytes);
    }
}
public record FeedbackRecord(string user_id, string feedback);
