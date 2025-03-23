using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Media;
using FluentPort.SDK;

namespace FluentPort.Views;

public partial class UpdateView : UserControl
{
    public UpdateView()
    {
        InitializeComponent();
    }

    public void SetVersionText(string currentVersion, string newVersion)
    {
        VersionText.Text = currentVersion + " --> " + newVersion;
    }

    private void UpdateBtnClicked(object? sender, RoutedEventArgs e)
    {
        string url = "https://www.fluentport.com/download";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
}
