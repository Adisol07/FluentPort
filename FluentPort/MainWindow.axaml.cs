using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluentPort.Views;
using FluentPort.Overlays;
using System;
using System.IO;
using System.Text.Json;
using FluentPort.SDK;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FluentPort;

public partial class MainWindow : Window
{
    public static string? AppPath;
    public static MainWindow? Instance;
    public static TunnelsView? TunnelsViewInstance;
    public static SettingsView? SettingsViewInstance;
    public static UpdateView? UpdateViewInstance;
    public static LoginView? LoginViewInstance;
    public static RegisterView? RegisterViewInstance;
    public static AccountView? AccountViewInstance;
    public static GiveFeedbackView? GiveFeedbackViewInstance;
    public static OfflineOverlay? OfflineOverlayInstance;
    public static ClosingOverlay? ClosingOverlayInstance;

    private static bool is_online = true;
    public static bool IsOnline
    {
        get => is_online;
        set
        {
            is_online = value;
            if (!value)
            {
                foreach (Tunnel tunnel in TunnelsViewInstance!.Tunnels.Keys)
                {
                    tunnel.Close();
                }
            }
        }
    }

    public MainWindow()
    {
        AppPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/FluentPort/";
        if (!Directory.Exists(AppPath))
            Directory.CreateDirectory(AppPath);
        InitializeComponent();

        Logger.OnLog += (log) =>
        {
            Console.WriteLine(log);
        };

        Instance = this;
        TunnelsViewInstance = new TunnelsView();
        SettingsViewInstance = new SettingsView();
        LoginViewInstance = new LoginView();
        RegisterViewInstance = new RegisterView();
        AccountViewInstance = new AccountView();
        GiveFeedbackViewInstance = new GiveFeedbackView();

        OfflineOverlayInstance = new OfflineOverlay();

        Task<(string currVer, string newVer)> check_version_task = Task.Run(check_version);
        Task.WhenAll(check_version_task);
        if (string.IsNullOrEmpty(check_version_task.Result.currVer) && string.IsNullOrEmpty(check_version_task.Result.newVer))
        {
            check_if_mobile_network();
            NetworkChange.NetworkAvailabilityChanged += (sender, args) =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    IsOnline = args.IsAvailable;
                    if (IsOnline)
                        check_if_mobile_network();
                    ToggleOverlay(OfflineOverlayInstance);
                });
            };

            this.Closing += (s, e) =>
            {
                bool found = false;
                foreach (Tunnel tunnel in MainWindow.TunnelsViewInstance!.Tunnels.Keys)
                {
                    if (tunnel.IsOpen)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return;
                ClosingOverlayInstance = new ClosingOverlay();
                ClosingOverlayInstance.ContinueClicked += () =>
                {
                    ToggleOverlay(ClosingOverlayInstance);
                    foreach (Tunnel tunnel in MainWindow.TunnelsViewInstance!.Tunnels.Keys)
                    {
                        tunnel.Close();
                    }
                    e.Cancel = false;
                    Environment.Exit(0);
                };
                ClosingOverlayInstance.CancelClicked += () =>
                {
                    ToggleOverlay(ClosingOverlayInstance);
                };
                ToggleOverlay(ClosingOverlayInstance);
                e.Cancel = true;
            };

            auto_login();
        }
        else if (check_version_task.Result.currVer == "ERROR" && check_version_task.Result.newVer == "ERROR")
        {
            UnavailableView unavailable_view = new UnavailableView();
            SwitchView(unavailable_view);
        }
        else
        {
            UpdateViewInstance = new UpdateView();
            UpdateViewInstance.SetVersionText(check_version_task.Result.currVer, check_version_task.Result.newVer);
            SwitchView(UpdateViewInstance);
        }
    }

    public static void SwitchView(UserControl view)
    {
        Instance?.MainFrame.Children.Clear();
        Instance?.MainFrame.Children.Add(view);

        Instance?.TunnelsMenuBtn.Classes.Clear();
        Instance?.TunnelsMenuBtn.Classes.Add("MenuBtn");
        Instance?.SettingsMenuBtn.Classes.Clear();
        Instance?.SettingsMenuBtn.Classes.Add("MenuBtn");
        Instance?.AddTunnelBtn.Classes.Clear();
        Instance?.AddTunnelBtn.Classes.Add("MenuBtn");

        MainWindow.Instance!.AddTunnelBtn.IsVisible = false;
        if (view is TunnelsView)
        {
            Instance?.TunnelsMenuBtn.Classes.Add("SelectedBtn");
            MainWindow.Instance!.HeaderBorder.IsVisible = true;
            MainWindow.Instance!.AddTunnelBtn.IsVisible = true;
            MainWindow.TunnelsViewInstance!.Reload();
            MainWindow.TunnelsViewInstance!.TunnelsScroll.ScrollToHome(); // does not work
        }
        else if (view is SettingsView)
        {
            Instance?.SettingsMenuBtn.Classes.Add("SelectedBtn");
            MainWindow.Instance!.HeaderBorder.IsVisible = true;
        }
        else if (view is AccountView)
        {
            MainWindow.Instance!.HeaderBorder.IsVisible = true;
            AccountViewInstance!.SaveInfoBtn.IsVisible = false;
        }
        else if (view is AddTunnelView)
        {
            Instance?.AddTunnelBtn.Classes.Add("SelectedBtn");
            MainWindow.Instance!.AddTunnelBtn.IsVisible = true;
        }
        else if (view is ManageTunnelView)
        {
        }
        else if (view is GiveFeedbackView)
        {
        }
        else
        {
            MainWindow.Instance!.HeaderBorder.IsVisible = false;
        }
    }
    public static void ToggleOverlay(IOverlay overlay)
    {
        if (Instance?.Overlay.Children.Count != 0 && Instance?.Overlay.Children[0] == overlay.UserControl)
            CloseOverlay();
        else
            OpenOverlay(overlay);
    }
    public static void OpenOverlay(IOverlay overlay)
    {
        Instance?.Overlay.Children.Clear();
        Instance?.Overlay.Children.Add(overlay.UserControl);
        MainWindow.Instance!.OverlayBorder.IsVisible = true;
        MainWindow.Instance!.OverlayBorder.PointerPressed += (s, e) =>
        {
            overlay.ClickAway.Invoke();
        };
    }
    public static void CloseOverlay()
    {
        MainWindow.Instance!.OverlayBorder.IsVisible = false;
    }

    public static void ReloadUserInfo()
    {
        MainWindow.Instance!.ProfileText.Text = UserInfo.Username;
        AccountViewInstance!.UsernameTB.Text = UserInfo.Username;
        AccountViewInstance!.EmailTB.Text = UserInfo.Email;
        if (!string.IsNullOrEmpty(UserInfo.Role))
        {
            MainWindow.Instance!.ProfileRole.Text = UserInfo.Role;
            MainWindow.Instance!.ProfileRole.IsVisible = true;

            AccountViewInstance.RoleText.IsVisible = true;
            AccountViewInstance.RoleTB.IsVisible = true;
            AccountViewInstance.RoleTB.Text = UserInfo.Role;
        }
        else
        {
            MainWindow.Instance!.ProfileRole.IsVisible = false;
            AccountViewInstance.RoleText.IsVisible = false;
            AccountViewInstance.RoleTB.IsVisible = false;
        }
        if (UserInfo.ProfilePicture != null && UserInfo.ProfilePicture.Length > 0)
        {
            MainWindow.Instance!.ProfilePicture.Source = new Bitmap(new MemoryStream(UserInfo.ProfilePicture));
            MainWindow.Instance!.ProfilePicture.IsVisible = true;
            MainWindow.Instance!.ProfilePictureBorder.Background = Brushes.Transparent;
        }
        else
        {
            MainWindow.Instance!.ProfilePicture.IsVisible = false;
            MainWindow.Instance!.ProfilePictureBorder.Background = Brushes.White;
        }
    }

    private void TunnelsMenuBtnClicked(object? sender, RoutedEventArgs e)
    {
        SwitchView(TunnelsViewInstance!);
    }
    private void SettingsMenuBtnClicked(object? sender, RoutedEventArgs e)
    {
        SwitchView(SettingsViewInstance!);
    }
    private void AccountBtnClicked(object? sender, RoutedEventArgs e)
    {
        SwitchView(AccountViewInstance!);
    }

    private void AddTunnelBtnClicked(object? sender, RoutedEventArgs e)
    {
        SwitchView(new AddTunnelView());
    }

    private async void auto_login()
    {
        if (File.Exists(AppPath + "/auth_token.txt"))
        {
            string token = File.ReadAllText(AppPath + "/auth_token.txt");
            APIResult? api_result = await User.Login(new LoginRequest(token, "", "", ""));
            if (api_result?.Status == APIResultStatus.Success && !string.IsNullOrEmpty(api_result?.Tag?.ToString()!))
            {
                Console.WriteLine("[OK]: AutoLogin api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
                string new_token = api_result?.Tag?.ToString()!;

                UserInfo.Token = new_token;
                File.WriteAllText(AppPath + "/auth_token.txt", new_token);
                if (!await UserInfo.Get())
                {
                    Console.WriteLine("[FAIL]: Get account info request failed, api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
                    SwitchView(LoginViewInstance!);
                    return;
                }
                MainWindow.ReloadUserInfo();

                MainWindow.SwitchView(MainWindow.TunnelsViewInstance!);
                return;
            }
            else
            {
                Console.WriteLine("[FAIL]: AutoLogin request failed, api result: \"" + JsonSerializer.Serialize(api_result) + "\"");
            }
        }
        SwitchView(LoginViewInstance!);
    }

    private void check_if_mobile_network()
    {
        string gateway = gateway_ip();
        if (gateway.StartsWith("192.168.43.") || gateway.StartsWith("172.20.10."))
        {
            MobileNetworkOverlay overlay = new MobileNetworkOverlay();
            overlay.ContinueClicked += () =>
            {
                ToggleOverlay(overlay);
            };
            ToggleOverlay(overlay);
        }
    }

    private string gateway_ip()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                foreach (GatewayIPAddressInformation gateway in ni.GetIPProperties().GatewayAddresses)
                    return gateway.Address.ToString();
        return "Unknown";
    }

    private async Task<(string currVer, string newVer)> check_version()
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "FluentPort SDK/1.0");
            HttpResponseMessage response = await client.GetAsync(Utils.API_SERVER + "/");
            if (response.IsSuccessStatusCode)
            {
                APIDetails? details = await response.Content.ReadFromJsonAsync<APIDetails>();
                if (details!.Version == Utils.Version)
                    return ("", "");

                return (Utils.Version!, details!.Version!);
            }
            else
            {
                return (null!, null!);
            }
        }
        catch
        {
            return ("ERROR", "ERROR");
        }
    }
}
