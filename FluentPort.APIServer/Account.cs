using System.Text.Json;
using FluentPort.APIServer.Models;
using FluentPort.SDK;

namespace FluentPort.APIServer;

public class Account
{
    public static string? UsersDirectory { get; set; }
    public static string? DataDirectory { get; set; }

    private string token = "";

    public string? ID { get; set; }
    public string? Token
    {
        get => token;
        set
        {
            if (!AuthModel.TokenToID.ContainsKey(value!))
            {
                AuthModel.TokenToID.Remove(token);
                AuthModel.TokenToID.Add(value!, ID!);
            }
            token = value!;
        }
    }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? PasswordHash { get; set; }
    public Dictionary<string, TunnelInfo>? Tunnels { get; set; }

    public Account()
    { }
    public Account(string id, string token, string username, string email, string role, string passwordHash, List<TunnelInfo> tunnels)
    {
        ID = id;
        Token = token;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Tunnels = new Dictionary<string, TunnelInfo>();
        foreach (TunnelInfo info in tunnels)
        {
            Tunnels!.Add(info.ID!, info);
        }
    }

    public static Account Load(string id)
    {
        string json = File.ReadAllText(UsersDirectory + "/" + id + "/info.json");
        return JsonSerializer.Deserialize<Account>(json)!;
    }
    public byte[] ReadProfilePicture()
    {
        return File.ReadAllBytes(UsersDirectory + "/" + ID + "/profile_icon.jpeg");
    }
    public void WriteProfilePicture(byte[] bytes)
    {
        File.WriteAllBytes(UsersDirectory + "/" + ID + "/profile_icon.jpeg", bytes);
    }
    public void Save()
    {
        if (!Directory.Exists(UsersDirectory + "/" + ID))
            Directory.CreateDirectory(UsersDirectory + "/" + ID);
        if (!File.Exists(UsersDirectory + "/" + ID + "/info.json"))
            File.WriteAllText(UsersDirectory + "/" + ID + "/info.json", "");
        if (!File.Exists(UsersDirectory + "/" + ID + "/profile_icon.jpeg"))
            File.Copy(DataDirectory + "/default_profile_icon.jpeg", UsersDirectory + "/" + ID + "/profile_icon.jpeg");

        if (!AuthModel.UsernameToID.ContainsKey(Username!))
        {
            AuthModel.UsernameToID.Add(Username!, ID!);
        }
        if (!AuthModel.EmailToID.ContainsKey(Email!))
        {
            AuthModel.EmailToID.Add(Email!, ID!);
        }
        if (!AuthModel.TokenToID.ContainsKey(Token!))
        {
            AuthModel.TokenToID.Add(Token!, ID!);
        }

        string json = JsonSerializer.Serialize(this);
        File.WriteAllText(UsersDirectory + "/" + ID + "/info.json", json);
    }
    public void Delete()
    {
        Directory.Move(UsersDirectory + "/" + ID, DataDirectory + "/deleted_users/" + ID);
        if (AuthModel.UsernameToID.ContainsKey(Username!))
        {
            AuthModel.UsernameToID.Remove(Username!);
        }
        if (AuthModel.EmailToID.ContainsKey(Email!))
        {
            AuthModel.EmailToID.Remove(Email!);
        }
        if (AuthModel.TokenToID.ContainsKey(Token!))
        {
            AuthModel.TokenToID.Remove(Token!);
        }
    }
}
