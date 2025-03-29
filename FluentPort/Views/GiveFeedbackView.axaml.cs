using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Text;
using System.Text.Json;
using Avalonia.Media;
using FluentPort.SDK;

namespace FluentPort.Views;

public partial class GiveFeedbackView : UserControl
{
    public GiveFeedbackView()
    {
        InitializeComponent();
    }

    public void Clear()
    {
        ResultMessageText.Text = "";
        ResultMessageText.IsVisible = false;
    }

    private async void SendBtnClicked(object? sender, RoutedEventArgs e)
    {
        SendBtn.IsEnabled = false;
        string text = MessageTB.Text!;
        if (string.IsNullOrEmpty(text))
        {
            ResultMessageText.Text = "Message cannot be empty";
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
            SendBtn.IsEnabled = true;
            return;
        }

        SendFeedbackRequest request = new SendFeedbackRequest(new LoginRequest(UserInfo.Token!, "", "", ""), text);
        APIResult? result = await User.SendFeedback(request);
        if (result!.Status == APIResultStatus.Success)
        {
            MessageTB.Text = "";
            ResultMessageText.Text = "Thank you for your feedback!";
            ResultMessageText.Foreground = Brushes.Green;
            ResultMessageText.IsVisible = true;
        }
        else
        {
            ResultMessageText.Text = result.Tag!.ToString();
            ResultMessageText.Foreground = Brushes.Red;
            ResultMessageText.IsVisible = true;
        }
        SendBtn.IsEnabled = true;
    }
}
