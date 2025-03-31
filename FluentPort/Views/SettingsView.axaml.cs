using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections;
using System.Text.Json;
using System.IO;

namespace FluentPort.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        load_default_themes();
        load_themes();
    }

    private void ThemeCBSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        MainWindow.Config!.Theme = ThemeCB.SelectedItem!.ToString()!;
        MainWindow.Config.Save(MainWindow.AppPath + "/config.json");
        MainWindow.LoadTheme();
    }

    private void load_themes()
    {
        string[] theme_files = Directory.GetFiles(MainWindow.AppPath + "/themes/", "*.json");
        foreach (string theme_file in theme_files)
        {
            Theme theme = JsonSerializer.Deserialize<Theme>(File.ReadAllText(theme_file))!;
            ThemeCB.Items.Add(theme.Name);
        }
        ThemeCB.SelectedItem = MainWindow.Config!.Theme!;
    }

    private void load_default_themes()
    {
        if (!Directory.Exists(MainWindow.AppPath + "/themes"))
            Directory.CreateDirectory(MainWindow.AppPath + "/themes");
        if (!File.Exists(MainWindow.AppPath + "/themes/dark.json"))
        {
            Theme theme = new Theme();
            theme.Name = "Dark";

            File.WriteAllText(MainWindow.AppPath + "/themes/dark.json", JsonSerializer.Serialize(theme, new JsonSerializerOptions() { WriteIndented = true }));
        }
        if (!File.Exists(MainWindow.AppPath + "/themes/light.json"))
        {
            Theme theme = new Theme();
            theme.Name = "Light";

            File.WriteAllText(MainWindow.AppPath + "/themes/light.json", JsonSerializer.Serialize(theme, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
