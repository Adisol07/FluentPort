using FluentPort.SDK;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace FluentPort.APIServer.Models;

public static class AuthModel
{
    public static string FeedbacksDir = "";
    public static string EmailPassword = "";
    public static Dictionary<string, string> TokenToID { get; set; } = new Dictionary<string, string>();
    public static Dictionary<string, string> EmailToID { get; set; } = new Dictionary<string, string>();
    public static Dictionary<string, string> UsernameToID { get; set; } = new Dictionary<string, string>();
    public static Dictionary<string, string> PasswordResetCodeToID { get; set; } = new Dictionary<string, string>();

    public static void Load(string userDir)
    {
        TokenToID.Clear();
        EmailToID.Clear();
        UsernameToID.Clear();
        PasswordResetCodeToID.Clear();
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

    public static void PasswordResetCode(string id, string email)
    {
        string code = GenerateToken(6).Replace("==", "");

        PasswordResetCodeToID.Add(code, id);

        const string sender_email = "no-reply@fluentport.com";
        const string smtp_server = "smtp.seznam.cz";

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress("FluentPort", sender_email));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = "Password Reset for FluentPort";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = "<h1 text-align=\"text-align:center;\">Password Reset</h1><p>You have requested a password reset.</p><p>This is your code: </p><h2 style=\"text-align: center;\">" + code + "</h2><p>If this was not you then ignore this e-mail or contact support@fluentport.com</p>"
        };

        using (SmtpClient smtp = new SmtpClient())
        {
            smtp.Timeout = 10000;
            smtp.Connect(smtp_server, 465, SecureSocketOptions.Auto);

            smtp.Authenticate(sender_email, EmailPassword);

            smtp.Send(message);
            smtp.Disconnect(true);
        }

        Logger.Log(new Log(LogLevels.Information, "Auth.PasswordResetCode", "Sent password reset code to: " + email + " for: " + id));
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

    public static string GenerateToken(int size = 32)
    {
        byte[] token_bytes = new byte[size];
        RandomNumberGenerator.Fill(token_bytes);
        return Convert.ToBase64String(token_bytes);
    }
}
public record FeedbackRecord(string user_id, string feedback);
