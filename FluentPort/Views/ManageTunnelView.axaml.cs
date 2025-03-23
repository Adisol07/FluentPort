using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls;
using FluentPort.Overlays;
using FluentPort.SDK;
using FluentPort.UserControls;
/*using AvaloniaWebView;*/

namespace FluentPort.Views;

public partial class ManageTunnelView : UserControl
{
    public Tunnel? Tunnel { get; private set; }
    public TunnelUserControl? UserControl { get; private set; }

    public ManageTunnelView()
    {
        InitializeComponent();

        // AvaloniaWebView.WebView webView = new WebView();
        // TestBox.Children.Add(webView);
        // webView.Url = new System.Uri("https://www.google.com/");
    }
    public ManageTunnelView(Tunnel tunnel, TunnelUserControl uc)
    {
        InitializeComponent();

        Tunnel = tunnel;
        UserControl = uc;
        RemoteAddressGrid.IsVisible = false;
        RemotePortGrid.IsVisible = false;
        RemoteServerGrid.IsVisible = false;
        LocalAddressTB.Text = tunnel.Info!.LocalAddress!;
        LocalPortTB.Text = tunnel.Info.LocalPort.ToString();
        LocalAddressTB.IsEnabled = !tunnel.IsOpen;
        LocalPortTB.IsEnabled = !tunnel.IsOpen;
        //if (!string.IsNullOrEmpty(tunnel.Subdomain))
        //{
        //    SubdomainTB.Text = tunnel.Subdomain;
        //}
        if (!string.IsNullOrEmpty(tunnel.Info.RemoteAddress) && tunnel.IsOpen)
        {
            RemoteAddressTB.Text = tunnel.Info!.RemoteAddress!;
            RemoteAddressGrid.IsVisible = true;
        }
        if (!string.IsNullOrEmpty(tunnel.ServerTarget) && tunnel.IsOpen)
        {
            RemoteServerTB.Text = tunnel.ServerTarget!;
            RemoteServerGrid.IsVisible = true;
        }
        if (tunnel.Info.RemotePort != 0 && tunnel.IsOpen)
        {
            RemotePortTB.Text = tunnel.Info.RemotePort.ToString();
            RemotePortGrid.IsVisible = true;
        }
        CreatedAtTB.Text = tunnel.Info.CreatedAt.LocalDateTime.ToString();
    }

    private void RemoveBtnClicked(object? sender, RoutedEventArgs e)
    {
        TunnelRemovalOverlay overlay = new TunnelRemovalOverlay();
        overlay.CancelClicked += () =>
        {
            MainWindow.ToggleOverlay(overlay);
        };
        overlay.DeleteClicked += async () =>
        {
            overlay.DeleteBtn.IsEnabled = false;
            overlay.DeleteBtn.Content = "Loading..";
            if (Tunnel!.IsOpen)
                Tunnel!.Close();
            APIResult? result = await Tunnel!.RemoveTunnel();
            if (result?.Message == "Success")
            {
                await UserInfo.Get();
                MainWindow.ToggleOverlay(overlay);
                MainWindow.SwitchView(MainWindow.TunnelsViewInstance!);
            }
            overlay.DeleteBtn.Content = "Deleted";
        };
        MainWindow.ToggleOverlay(overlay);
    }

    private void AssignSubdomainBtnClicked(object? sender, RoutedEventArgs e)
    {
        //APIResult? result = await Tunnel!.AssignSubdomain(SubdomainTB.Text!);
    }

    private async void SaveBtnClicked(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(LocalPortTB.Text, out int port) && port <= 65535 && port > 0)
        {
            Tunnel!.Info!.LocalAddress = LocalAddressTB.Text;
            Tunnel!.Info!.LocalPort = port;
            APIResult? result = await Tunnel.SaveTunnel();
            if (result!.Status == APIResultStatus.Success)
            {
                SaveBtn.IsVisible = false;
                SaveMsgText.IsVisible = false;
                UserControl!.ReloadInfo();
            }
            else
            {
                SaveMsgText.IsVisible = true;
                SaveMsgText.Foreground = Brushes.Red;
                SaveMsgText.Text = result!.Tag!.ToString()!;
            }
        }
        else
        {
            SaveMsgText.IsVisible = true;
            SaveMsgText.Foreground = Brushes.Red;
            SaveMsgText.Text = "Port is invalid!";
        }
    }

    private void LocalAddressTBTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Tunnel!.Info!.LocalAddress != LocalAddressTB.Text)
            SaveBtn.IsVisible = true;
    }
    private void LocalPortTBTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Tunnel!.Info!.LocalPort.ToString() != LocalPortTB.Text)
            SaveBtn.IsVisible = true;
    }
    private void RemoteAddressTBTextChanged(object? sender, TextChangedEventArgs e)
    { }
    private void RemotePortTBTextChanged(object? sender, TextChangedEventArgs e)
    { }
    private void CreatedAtTBTextChanged(object? sender, TextChangedEventArgs e)
    { }
    private void RemoteServerTBTextChanged(object? sender, TextChangedEventArgs e)
    { }
}

