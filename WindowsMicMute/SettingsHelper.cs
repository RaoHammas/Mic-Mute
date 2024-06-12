using System.IO;
using System.Text.Json;
using System.Windows;

namespace WindowsMicMute;

public sealed class SettingsHelper
{
    public static SettingsHelper Instance { get; } = new();

    private const string SettingsFileName = "Settings.json";
    private readonly string _settingsFilePath;

    private SettingsHelper()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
        EnsureSettingsFileExists();
    }


    private void EnsureSettingsFileExists()
    {
        if (!File.Exists(_settingsFilePath))
        {
            var defaultSettings = new WindowSettings();
            SaveSettings(defaultSettings);
        }
    }

    public void SaveWindowLocation(Window window)
    {
        var settings = new WindowSettings
        {
            Top = window.Top,
            Left = window.Left,
            Width = window.Width,
            Height = window.Height
        };

        SaveSettings(settings);
    }

    public WindowSettings? LoadWindowLocation()
    {
        var json = File.ReadAllText(_settingsFilePath);
        return JsonSerializer.Deserialize<WindowSettings>(json);
    }

    private void SaveSettings(WindowSettings settings)
    {
        if (double.IsNaN(settings.Height) || double.IsNaN(settings.Width))
        {
            return;
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }
}

public class WindowSettings
{
    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}