using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentPort.SDK;
using FluentPort.Overlays;
using System;

namespace FluentPort.Views;

public partial class AddTunnelView : UserControl
{
    public AddTunnelView()
    {
        InitializeComponent();

        LocationCB.Items.Add("auto");
        LocationCB.Items.Add("Frankfurt, Germany");
        LocationCB.SelectedIndex = 0;
    }

    private void LocationCBSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        BillingMessageText!.IsVisible = LocationCB.SelectedItem!.ToString() != "auto";
    }

    private void LocalConnectionHelpBtnClicked(object? sender, RoutedEventArgs e)
    {
        AddTunnelHelpOverlay overlay = new AddTunnelHelpOverlay();
        overlay.ContinueClicked += () =>
        {
            MainWindow.ToggleOverlay(overlay);
        };
        MainWindow.ToggleOverlay(overlay);
    }

    private async void CreateBtnClicked(object? sender, RoutedEventArgs e)
    {
        string local = LocalTB.Text!;
        if (string.IsNullOrEmpty(local))
        {
            ResultMessageText!.Text = "You must at least provide a port";
            ResultMessageText!.Foreground = Brushes.Red;
            ResultMessageText!.IsVisible = true;
            return;
        }
        if (local.StartsWith(":"))
            local = local.Remove(0, 1);
        if (!local.Contains(":"))
            local = "127.0.0.1:" + local;
        string location = LocationCB.SelectedItem!.ToString()!;

        Tunnel tunnel = new Tunnel(
                new TunnelInfo(Guid.NewGuid().ToString(), local.Split(":")[0], "", Convert.ToInt32(local.Split(":")[1]), 0, DateTimeOffset.Now),
                UserInfo.Token!);
        APIResult? result = await tunnel.SaveTunnel();
        if (result?.Message == "Success")
        {
            await UserInfo.Get();
            MainWindow.SwitchView(MainWindow.TunnelsViewInstance!);
            MainWindow.TunnelsViewInstance!.TunnelsScroll.ScrollToEnd();
        }
        else if (result?.Message == "Limit")
        {
            ResultMessageText.Text = "You have reached the limit of " + result.Tag + " created tunnels!";
            ResultMessageText.IsVisible = true;
            ResultMessageText.Foreground = Brushes.Red;
        }
    }
}
