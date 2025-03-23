using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Media;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System;
using System.IO;
using System.Text.Json;
using FluentPort.SDK;
using System.Runtime.InteropServices;

namespace FluentPort.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();
    }

    private async void RegisterBtnClicked(object? sender, RoutedEventArgs e)
    {
        RegisterBtn.IsEnabled = false;
        string username = UsernameTB.Text!;
        string email = EmailTB.Text!;
        string password = PasswordTB.Text!;
        bool? terms = TermsCKB?.IsChecked;
        bool? keep_signed = KeepSignedCKB?.IsChecked;

        if (terms == false)
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Agreement to Terms of Service is required for registration";
            ResultMessageText.Foreground = Brushes.Red;
            RegisterBtn.IsEnabled = true;
            return;
        }
        else if (string.IsNullOrEmpty(username))
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Username is invalid!";
            ResultMessageText.Foreground = Brushes.Red;
            RegisterBtn.IsEnabled = true;
            return;
        }
        else if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "E-mail is invalid!";
            ResultMessageText.Foreground = Brushes.Red;
            RegisterBtn.IsEnabled = true;
            return;
        }
        else if (string.IsNullOrEmpty(password))
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Password cannot be empty!";
            ResultMessageText.Foreground = Brushes.Red;
            RegisterBtn.IsEnabled = true;
            return;
        }

        string password_hash = BCrypt.Net.BCrypt.HashPassword(password);
        APIResult? api_result = await User.Register(new LoginRequest("", username, email.ToLower(), password_hash));
        if (!string.IsNullOrEmpty(api_result?.Tag?.ToString()!))
        {
            Console.WriteLine("[OK]: Register api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
            string token = api_result?.Tag?.ToString()!;
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Successfully signed up";
            ResultMessageText.Foreground = Brushes.Green;
            EmailTB.Text = "";
            UsernameTB.Text = "";
            PasswordTB.Text = "";

            UserInfo.Token = token;
            if (!await UserInfo.Get())
            {
                ResultMessageText.Text = "An error occured";
                ResultMessageText.Foreground = Brushes.Red;
                Console.WriteLine("[FAIL]: Get account info request failed, api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
                return;
            }
            if (keep_signed == true)
            {
                File.WriteAllText(MainWindow.AppPath + "/auth_token.txt", token);
            }
            MainWindow.ReloadUserInfo();

            MainWindow.SwitchView(MainWindow.TunnelsViewInstance!);
            ResultMessageText.IsVisible = false;
        }
        else
        {
            Console.WriteLine("[FAIL]: Register request failed, api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = api_result?.Message;
            ResultMessageText.Foreground = Brushes.Red;
        }
        RegisterBtn.IsEnabled = true;
    }

    private void SignInLinkClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(MainWindow.LoginViewInstance!);
    }

    private void TermsClicked(object? sender, RoutedEventArgs e)
    {
        open_url("https://www.fluentport.com/terms-of-service");
    }
    private void PrivacyClicked(object? sender, RoutedEventArgs e)
    {
        open_url("https://www.fluentport.com/privacy-policy");
    }

    private static void open_url(string url)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else
            {
                throw new PlatformNotSupportedException("Cannot open URL on this platform");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[FAIL] Error opening URL: " + ex.Message);
        }
    }
}
