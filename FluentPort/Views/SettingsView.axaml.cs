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

        load_default_languages();
        load_languages();
        load_default_themes();
        load_themes();
    }

    private void LanguageCBSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {

    }

    private void load_languages()
    {
        string[] language_files = Directory.GetFiles(MainWindow.AppPath + "/languages/", "*.json");
        foreach (string language_file in language_files)
        {
            Language lang = JsonSerializer.Deserialize<Language>(File.ReadAllText(language_file))!;
            LanguageCB.Items.Add(lang.Name);
        }
        LanguageCB.SelectedItem = MainWindow.Config!.Language!;
    }

    private void load_default_languages()
    {
        if (!Directory.Exists(MainWindow.AppPath + "/languages"))
            Directory.CreateDirectory(MainWindow.AppPath + "/languages");

        if (!File.Exists(MainWindow.AppPath + "/languages/english.json"))
        {
            Language theme = new Language();
            theme.Name = "English";

            File.WriteAllText(MainWindow.AppPath + "/languages/english.json", JsonSerializer.Serialize(theme, new JsonSerializerOptions() { WriteIndented = true }));
        }
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

            theme.PrimaryColor = "#3772FF";
            theme.SecondaryColor = "#DF2935";
            theme.RaisinBlackColor = "#2D2A32";
            theme.WhiteSmokeColor = "#F5F3F5";
            theme.MintGreenColor = "#E9FFF9";

            File.WriteAllText(MainWindow.AppPath + "/themes/dark.json", JsonSerializer.Serialize(theme, new JsonSerializerOptions() { WriteIndented = true }));
        }
        if (!File.Exists(MainWindow.AppPath + "/themes/light.json"))
        {
            Theme theme = new Theme();
            theme.Name = "Light";

            theme.PrimaryColor = "#3772FF";
            theme.SecondaryColor = "#DF2935";
            theme.RaisinBlackColor = "#F5F3F5";
            theme.WhiteSmokeColor = "#2D2A32";
            theme.MintGreenColor = "#E9FFF9";

            File.WriteAllText(MainWindow.AppPath + "/themes/light.json", JsonSerializer.Serialize(theme, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
