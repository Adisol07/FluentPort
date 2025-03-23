using Avalonia.Controls;
using FluentPort.UserControls;
using FluentPort.SDK;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Collections.Generic;

namespace FluentPort.Views;

public partial class TunnelsView : UserControl
{
    public Dictionary<Tunnel, TunnelUserControl> Tunnels = new Dictionary<Tunnel, TunnelUserControl>();

    public TunnelsView()
    {
        InitializeComponent();
    }

    public void Reload()
    {
        //Tunnel.PrivateKey = Utils.DecryptString(Utils.PRIVATE_KEY, "51673631-5c8e-442f-8947-d2ea7fdaf3bc");
        //Tunnel.UserToken = UserInfo.Token!;

        _ = Task.Run(async () =>
        {
            while (UserInfo.Tunnels == null)
            {
                Console.WriteLine("Waiting for tunnels information..");
                await Task.Delay(100);
            }
            Dispatcher.UIThread.Invoke(() =>
            {
                TutorialText.IsVisible = UserInfo.Tunnels.Count == 0;
                foreach (TunnelInfo info in UserInfo.Tunnels!)
                {
                    bool found = false;
                    foreach (var t in Tunnels)
                    {
                        if (t.Key.Info!.ID! == info.ID!)
                            found = true;
                    }
                    if (!found)
                    {
                        Tunnel tunnel = new Tunnel(info, UserInfo.Token!);
                        AddTunnel(tunnel);
                    }
                }
                Dictionary<Tunnel, TunnelUserControl> to_remove = new Dictionary<Tunnel, TunnelUserControl>();
                foreach (var t in Tunnels)
                {
                    bool found = false;
                    foreach (TunnelInfo info in UserInfo.Tunnels!)
                    {
                        if (t.Key.Info!.ID! == info.ID!)
                        {
                            found = true;
                            t.Key.Info.LocalAddress = info.LocalAddress;
                            t.Key.Info.LocalPort = info.LocalPort;
                            t.Key.Info.RemoteAddress = info.RemoteAddress;
                            t.Key.Info.RemotePort = info.RemotePort;
                            break;
                        }
                    }
                    if (!found)
                    {
                        to_remove.Add(t.Key, t.Value);
                    }
                }
                foreach (var t in to_remove)
                {
                    TunnelsPanel.Children.Remove(t.Value);
                    Tunnels.Remove(t.Key);
                }
            });
        });
    }

    public void AddTunnel(Tunnel tunnel)
    {
        TunnelUserControl uc = new TunnelUserControl(tunnel);
        TunnelsPanel.Children.Add(uc);
        TunnelsScroll.ScrollToEnd();
        Tunnels.Add(tunnel, uc);
    }
}
