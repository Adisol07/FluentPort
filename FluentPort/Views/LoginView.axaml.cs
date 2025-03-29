using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System;
using System.IO;
using Avalonia.Media;
using FluentPort.SDK;

namespace FluentPort.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void LoginBtnClicked(object? sender, RoutedEventArgs e)
    {
        LoginBtn.IsEnabled = false;
        string email = EmailTB.Text!;
        string password = PasswordTB.Text!;
        bool? keep_signed = KeepSignedCKB?.IsChecked;

        if (string.IsNullOrEmpty(email))// || !email.Contains("@") || !email.Contains("."))
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "E-mail is invalid!";
            ResultMessageText.Foreground = Brushes.Red;
            LoginBtn.IsEnabled = true;
            return;
        }
        else if (string.IsNullOrEmpty(password))
        {
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Password cannot be empty!";
            ResultMessageText.Foreground = Brushes.Red;
            LoginBtn.IsEnabled = true;
            return;
        }

        APIResult? api_result = await User.Login(new LoginRequest("", email, email.ToLower(), password));
        if (!string.IsNullOrEmpty(api_result?.Tag?.ToString()!))
        {
            Console.WriteLine("[OK]: Login api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
            string token = api_result?.Tag?.ToString()!;
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = "Successfully logged in";
            ResultMessageText.Foreground = Brushes.Green;
            EmailTB.Text = "";
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
            Console.WriteLine("[FAIL]: Login request failed, api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
            ResultMessageText.IsVisible = true;
            ResultMessageText.Text = api_result?.Message;
            ResultMessageText.Foreground = Brushes.Red;
        }
        LoginBtn.IsEnabled = true;
    }

    private void SignUpLinkClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(MainWindow.RegisterViewInstance!);
    }

    private void ResetPasswordLinkClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(MainWindow.ResetPasswordRequestViewInstance!);
    }
}
