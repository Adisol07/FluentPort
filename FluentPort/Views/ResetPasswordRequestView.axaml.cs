using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentPort.SDK;
using FluentPort.Overlays;
using System;

namespace FluentPort.Views;

public partial class ResetPasswordRequestView : UserControl
{
    public ResetPasswordRequestView()
    {
        InitializeComponent();
    }

    private async void RequestPasswordResetBtnClicked(object? sender, RoutedEventArgs e)
    {
        RequestPasswordResetBtn.IsEnabled = false;
        string email = EmailTB.Text!;
        if (string.IsNullOrEmpty(email))
        {
            ResultMessageText.Text = "E-mail address cannot be empty!";
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            RequestPasswordResetBtn.IsEnabled = true;
            return;
        }
        APIResult? result = await User.RequestPasswordResetRequest(new RequestPasswordResetRequest(email));
        if (result!.Status == APIResultStatus.Success)
        {
            RequestPasswordResetBtn.IsEnabled = true;
            MainWindow.SwitchView(MainWindow.ResetPasswordViewInstance!);
        }
        else
        {
            RequestPasswordResetBtn.IsEnabled = true;
            ResultMessageText.Text = result!.Tag!.ToString();
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            return;
        }
    }

    private void BackBtnClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(MainWindow.LoginViewInstance!);
    }
}
