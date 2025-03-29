using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPort.SDK;
using System;
using FluentPort.Views;
using FluentPort.Overlays;

namespace FluentPort.UserControls;

public partial class TunnelUserControl : UserControl
{
    public Tunnel? Tunnel { get; set; }
    public string? LocalAdddress
    {
        get
        {
            return LocalAddressText.Text;
        }
        set
        {
            LocalAddressText.Text = value;
        }
    }
    public string? RemoteAddress
    {
        get
        {
            return RemoteAddressText.Text;
        }
        set
        {
            RemoteAddressLink.IsVisible = !string.IsNullOrEmpty(value);
            LevelIndicatorPicture.IsVisible = string.IsNullOrEmpty(value);
            RemoteAddressText.Text = value;
        }
    }
    public int TotalRequests { get; private set; }
    public bool Running { get; private set; }
    public DateTime EndTime { get; private set; }

    public TunnelUserControl()
    {
        InitializeComponent();
    }
    public TunnelUserControl(Tunnel tunnel)
    {
        InitializeComponent();

        Tunnel = tunnel;
        tunnel.OnStart += () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Running = true;
                EndTime = DateTime.Now + TimeSpan.FromDays(30);

                RemoteAddress = tunnel.Info!.RemoteAddress! + ":" + tunnel.Info.RemotePort;
                StartBtn.Classes.Add("RedBtn");
                StartBtn.Classes.Remove("SelectedBtn");
                StartBtn.Content = "Stop";

                ArrowLeft.IsVisible = true;
                ArrowMiddle1.IsVisible = true;
                ArrowMiddle.IsVisible = true;
                ArrowMiddle2.IsVisible = true;
                ArrowRight.IsVisible = true;

                StartBtn.IsEnabled = true;

                timer();
            });
        };
        tunnel.OnStop += () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Running = false;

                RemoteAddress = "";
                StartBtn.Classes.Remove("RedBtn");
                StartBtn.Classes.Add("SelectedBtn");
                StartBtn.Content = "Start";

                ArrowLeft.IsVisible = false;
                ArrowMiddle1.IsVisible = false;
                ArrowMiddle.IsVisible = false;
                ArrowMiddle2.IsVisible = false;
                ArrowRight.IsVisible = false;
                ArrowStatus.Text = "";

                StartBtn.IsEnabled = true;
            });
        };
        tunnel.ClientConnected += (guid) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                TotalRequests++;
            });
        };
        tunnel.ClientDisconnected += (guid) => { };
        tunnel.PacketReceived += (packet) =>
        {
            //Dispatcher.UIThread.Invoke(() =>
            //{
            //Ping = (int)((DateTimeOffset.UtcNow - packet.Sent).TotalMilliseconds);

            //RemoteAddressText.Foreground = new SolidColorBrush(Color.Parse("#3772FF"));
            //});
            // _ = Task.Run(async () =>
            // {
            //     await Task.Delay(500);
            //     Dispatcher.UIThread.Invoke(() =>
            //     {
            //         RemoteAddressText.Foreground = new SolidColorBrush(Color.Parse("#F5F3F5"));
            //     });
            // });
        };
        ReloadInfo();
    }

    public void ReloadInfo()
    {
        LocalAdddress = Tunnel!.Info!.LocalAddress! + ":" + Tunnel.Info.LocalPort;
        RemoteAddress = "";
        if (!string.IsNullOrEmpty(Tunnel.Info!.RemoteAddress!))
            RemoteAddress = Tunnel.Info!.RemoteAddress! + ":" + Tunnel.Info.RemotePort;
    }

    private async void timer()
    {
        while (Running)
        {
            //long ping = await ping_address(Tunnel?.Info!.RemoteAddress!);
            ArrowStatus.Text = time_remaining(EndTime) + " left – " + Tunnel!.Ping + " ms";
            if ((EndTime - DateTime.Now).TotalSeconds <= 0)
            {
                Tunnel!.Close();
            }

            await Task.Delay(5000);
        }
    }
    private string time_remaining(DateTime datetime)
    {
        TimeSpan span = datetime - DateTime.Now;
        string result = span.ToString();

        if (Math.Floor(span.TotalSeconds) > 0)
        {
            result = Math.Floor(span.TotalSeconds) + " seconds";
        }
        if (Math.Floor(span.TotalMinutes) > 0)
        {
            result = Math.Floor(span.TotalMinutes) + " minutes";
        }
        if (Math.Floor(span.TotalHours) > 0)
        {
            result = Math.Floor(span.TotalHours) + " hours";
        }
        if (Math.Floor(span.TotalDays) > 0)
        {
            result = Math.Floor(span.TotalDays) + " days";
        }

        return result;
    }
    // private async Task<long> ping_address(string address)
    // {
    //     try
    //     {
    //         using (HttpClient client = new HttpClient())
    //         {
    //             Stopwatch stopwatch = Stopwatch.StartNew();
    //             HttpResponseMessage response = await client.GetAsync("http://" + address + ":666");
    //             stopwatch.Stop();

    //             if (response.IsSuccessStatusCode)
    //                 return stopwatch.ElapsedMilliseconds;
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine("Ping error: " + ex.Message);
    //     }
    //     return -1;
    // }
    private async void StartBtnClicked(object? sender, RoutedEventArgs e)
    {
        StartBtn.IsEnabled = false;
        StartBtn.Content = "Loading..";
        await Task.Run(async () =>
        {
            if (Tunnel!.IsOpen)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    StartBtn.Content = "Stopping..";
                });
                Tunnel.Close();
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    StartBtn.Content = "Starting..";
                });
                APIResult? result = await Tunnel.Open();
                if (result != null && result.Message == "Full")
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        AllServersAreFullOverlay overlay = new AllServersAreFullOverlay();
                        overlay.ContinueClicked += () =>
                        {
                            MainWindow.ToggleOverlay(overlay);
                            StartBtn.Content = "Start";
                            StartBtn.IsEnabled = true;
                        };
                        MainWindow.ToggleOverlay(overlay);
                    });
                }
            }
        });
    }
    private void ManageBtnClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.SwitchView(new ManageTunnelView(Tunnel!, this));
    }

    private async void RemoteAddressLinkClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        await clipboard!.SetTextAsync(RemoteAddress);
    }
    private async void LocalAddressLinkClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        await clipboard!.SetTextAsync(LocalAdddress);
    }
}
