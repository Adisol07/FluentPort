using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Threading.Tasks;
using FluentPort.SDK;
using FluentPort.Overlays;
using System;

namespace FluentPort.Views;

public partial class ResetPasswordView : UserControl
{
    public string? Code;

    public ResetPasswordView()
    {
        InitializeComponent();
    }

    private async void PasswordChangeBtnClicked(object? sender, RoutedEventArgs e)
    {
        PasswordChangeBtn.IsEnabled = false;
        string new_password = NewPasswordTB.Text!;
        if (string.IsNullOrEmpty(new_password))
        {
            PasswordChangeBtn.IsEnabled = true;
            return;
        }
        string password_hash = BCrypt.Net.BCrypt.HashPassword(new_password);
        APIResult? result = await User.ChangePassword(new ChangePasswordRequest(new LoginRequest(Code!, "", "", ""), password_hash));
        if (result!.Status == APIResultStatus.Success)
        {
            Code = "";
            ResultMessageText.Text = "Success, login with your new password";
            ResultMessageText.Foreground = Brushes.Green;
            ResultMessageText.IsVisible = true;
            await Task.Delay(1000);
            MainWindow.SwitchView(MainWindow.LoginViewInstance!);
            ResultMessageText.IsVisible = false;
            return;
        }
        else
        {
            PasswordChangeBtn.IsEnabled = true;
            ResultMessageText.Text = result!.Tag!.ToString();
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            return;
        }
    }

    private async void CheckCodeBtnClicked(object? sender, RoutedEventArgs e)
    {
        CheckCodeBtn.IsEnabled = false;
        string code = CodeTB.Text!;
        if (string.IsNullOrEmpty(code))
        {
            CheckCodeBtn.IsEnabled = true;
            return;
        }
        APIResult? result = await User.CheckPasswordResetCode(new CheckPasswordResetCodeRequest(code));
        if (result!.Status == APIResultStatus.Success)
        {
            ResultMessageText.IsVisible = false;
            CheckCodeBtn.IsEnabled = true;
            Code = code;
            CodeTB.IsVisible = false;
            NewPasswordTB.IsVisible = true;
            CheckCodeBtn.IsVisible = false;
            PasswordChangeBtn.IsVisible = true;
        }
        else
        {
            CheckCodeBtn.IsEnabled = true;
            ResultMessageText.Text = result!.Tag!.ToString();
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            return;
        }
    }

    private void BackBtnClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(MainWindow.ResetPasswordRequestViewInstance!);
    }
}
