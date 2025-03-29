using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Threading;
using FluentPort.SDK;
using System.Net;
using System.Net.Http;
using System.IO.Compression;

namespace FluentPort.Views;

public partial class UpdateView : UserControl
{
    public string? DownloadLink { get; private set; }
    public string? UserPath { get; private set; }
    public string? AppPath { get; private set; }
    public string? CurrentVersion { get; private set; }
    public string? NewVersion { get; private set; }

    public UpdateView()
    {
        InitializeComponent();
    }

    public void SetVersion(string currentVersion, string newVersion, string downloadLink, string appPath, string userPath)
    {
        DownloadLink = downloadLink;
        AppPath = appPath;
        UserPath = userPath;
        CurrentVersion = currentVersion;
        NewVersion = newVersion;
        VersionText.Text = currentVersion + " --> " + newVersion;
    }

    private async void UpdateBtnClicked(object? sender, RoutedEventArgs e)
    {
        UpdateBtn.IsEnabled = false;
        UpdateBtn.Content = "Updating..";

        try
        {
            ResultMessageText.Text = "Downloading..";
            ResultMessageText.Foreground = Brushes.Gray;
            ResultMessageText.IsVisible = true;
            await download_file_async(DownloadLink!, UserPath! + "/update_" + NewVersion! + ".zip");

            ResultMessageText.Text = "Installing..";
            extract_zip(UserPath! + "/update_" + NewVersion! + ".zip", AppPath!);
            ResultMessageText.Text = "Restarting..";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(AppPath! + "/FluentPort.exe");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start(new ProcessStartInfo(AppPath! + "/FluentPort.app") { UseShellExecute = true });
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            UpdateBtn.Content = "Update failed";
            UpdateBtn.IsEnabled = true;
            ResultMessageText.Text = ex.Message;
            ResultMessageText.Foreground = Brushes.Red;
        }
    }

    async Task download_file_async(string url, string outputPath)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            long total_bytes = response!.Content!.Headers!.ContentLength!.Value!;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var file_stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[8192];
                long total_read = 0;
                int bytes_read;

                while ((bytes_read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    total_read += bytes_read;
                    await file_stream.WriteAsync(buffer, 0, bytes_read);

                    double progress = (double)total_read / total_bytes * 100;
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ResultMessageText.Text = "Downloading " + Math.Round(progress) + "%";
                    });
                }
            }
        }
    }

    void extract_zip(string zipPath, string extractPath)
    {
        if (!Directory.Exists(extractPath))
            Directory.CreateDirectory(extractPath);

        ZipFile.ExtractToDirectory(zipPath, extractPath);
    }
}
