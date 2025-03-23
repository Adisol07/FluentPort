using Avalonia.Interactivity;
using Avalonia.Controls;
using System.IO;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using FluentPort.SDK;
using FluentPort.Overlays;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using System;

namespace FluentPort.Views;

public partial class AccountView : UserControl
{
    public AccountView()
    {
        InitializeComponent();
    }

    private void GiveFeedbackBtnClicked(object? sender, RoutedEventArgs e)
    {
        MainWindow.GiveFeedbackViewInstance!.Clear();
        MainWindow.SwitchView(MainWindow.GiveFeedbackViewInstance!);
    }

    private void ChangePasswordBtnClicked(object? sender, RoutedEventArgs e)
    {
        ChangePasswordOverlay overlay = new ChangePasswordOverlay();
        overlay.CancelClicked += () =>
        {
            MainWindow.ToggleOverlay(overlay);
        };
        MainWindow.ToggleOverlay(overlay);
    }

    private void SignOutBtnClicked(object? sender, RoutedEventArgs e)
    {
        SignOutBtn.IsEnabled = false;
        signout();
        SignOutBtn.IsEnabled = true;
    }
    private void signout()
    {
        foreach (var pair in MainWindow.TunnelsViewInstance!.Tunnels)
        {
            MainWindow.TunnelsViewInstance!.TunnelsPanel.Children.Remove(pair.Value);
            pair.Key.Close();
        }
        MainWindow.TunnelsViewInstance!.Tunnels.Clear();

        UserInfo.Username = "";
        UserInfo.Email = "";
        UserInfo.Token = "";
        UserInfo.Role = "";
        UserInfo.ProfilePicture = [];
        UserInfo.Tunnels!.Clear();

        File.WriteAllText(MainWindow.AppPath + "/auth_token.txt", "");
        MainWindow.SwitchView(MainWindow.LoginViewInstance!);
    }
    private async void ChangeProfileIconBtnClicked(object? sender, RoutedEventArgs e)
    {
        ChangeProfileIconBtn.IsEnabled = false;
        var top_level = TopLevel.GetTopLevel(this);
        var files = await top_level!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/bmp", "image/gif", "image/webp" }
                }
            }
        });
        ChangeProfileIconBtn.IsEnabled = true;
        if (files.Count < 1) return;

        var file = files[0];
        await using var stream = await file.OpenReadAsync();
        var bitmap = new Bitmap(stream);
        byte[] profile_picture = ConvertToJpeg(file.Path.LocalPath, bitmap);
        UserInfo.ProfilePicture = profile_picture;
        await UserInfo.Set();
        MainWindow.ReloadUserInfo();
    }
    private void RemoveBtnClicked(object? sender, RoutedEventArgs e)
    {
        RemoveBtn.IsEnabled = false;
        DeleteAccountOverlay overlay = new DeleteAccountOverlay();
        overlay.CancelClicked += () =>
        {
            MainWindow.ToggleOverlay(overlay);
        };
        overlay.DeleteClicked += async () =>
        {
            overlay.DeleteBtn.IsEnabled = false;
            overlay.DeleteBtn.Content = "Loading..";
            APIResult? result = await UserInfo.DeleteAccount();
            if (result?.Status == APIResultStatus.Success)
            {
                overlay.DeleteBtn.Content = "Deleted";
                MainWindow.ToggleOverlay(overlay);
                signout();
            }
            else
            {
                overlay.DeleteBtn.Content = result!.Tag!.ToString()!;
            }
            overlay.DeleteBtn.IsEnabled = true;
        };
        MainWindow.ToggleOverlay(overlay);
        RemoveBtn.IsEnabled = true;
    }

    private async void SaveInfoBtnClicked(object? sender, RoutedEventArgs e)
    {
        SaveInfoBtn.IsEnabled = false;
        UserInfo.Username = UsernameTB.Text;
        UserInfo.Email = EmailTB.Text;
        APIResult? result = await UserInfo.Set();
        if (result!.Status == APIResultStatus.Success)
        {
            MainWindow.ReloadUserInfo();
            SaveInfoBtn.IsVisible = false;
            SaveMsgText.IsVisible = false;
        }
        else
        {
            SaveMsgText.IsVisible = true;
            SaveMsgText.Foreground = Brushes.Red;
            SaveMsgText.Text = result!.Tag!.ToString()!;
        }
        SaveInfoBtn.IsEnabled = true;
    }

    private void UsernameTBTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (UserInfo.Username != UsernameTB.Text)
            SaveInfoBtn.IsVisible = true;
    }
    private void EmailTBTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (UserInfo.Email != EmailTB.Text)
            SaveInfoBtn.IsVisible = true;
    }

    public static async Task<byte[]> ReadFileAsBytesAsync(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        using var memory_stream = new MemoryStream();
        await stream.CopyToAsync(memory_stream);
        return memory_stream.ToArray();
    }

    public byte[] ConvertToJpeg(string inputPath, Bitmap bitmap)
    {
        using (var skBitmap = SKBitmap.Decode(inputPath))
        {
            if (skBitmap == null) return Array.Empty<byte>();

            using (var image = SKImage.FromBitmap(skBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80))
            {
                return data.ToArray();
            }
        }
    }
}

