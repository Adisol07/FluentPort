using Avalonia.Controls;
using System;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentPort.SDK;

namespace FluentPort.Overlays;

public partial class ChangePasswordOverlay : UserControl, IOverlay
{
    public Action ClickAway => () => { };
    public UserControl UserControl => this;

    public Action? CancelClicked;

    public ChangePasswordOverlay()
    {
        InitializeComponent();
    }

    private void CancelBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (CancelClicked != null)
            CancelClicked.Invoke();
    }
    private async void ProceedBtnClicked(object? sender, RoutedEventArgs e)
    {
        ProceedBtn.IsEnabled = false;
        string currentPassword = CurrentPasswordTB.Text!;
        string newPassword = NewPasswordTB.Text!;
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            ResultMessageText.Text = "Password can't be empty";
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            ProceedBtn.IsEnabled = true;
            return;
        }

        string new_password_hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        ChangePasswordRequest request = new ChangePasswordRequest(new LoginRequest(UserInfo.Token!, "", "", currentPassword), new_password_hash);
        APIResult? result = await User.ChangePassword(request);
        if (result!.Status == APIResultStatus.Success)
        {
            CurrentPasswordTB.Text = "";
            NewPasswordTB.Text = "";
            ResultMessageText.Text = "Password has been changed";
            ResultMessageText.Foreground = Brushes.Green;
            ResultMessageText.IsVisible = true;
        }
        else
        {
            ResultMessageText.Text = result.Tag!.ToString();
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
        }
        ProceedBtn.IsEnabled = true;
    }
}
