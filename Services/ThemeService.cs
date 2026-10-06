using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Win32;

namespace CellVera.Services;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CellVera");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    private static bool _initialized;

    public static ThemeMode Preference { get; private set; } = ThemeMode.System;
    public static bool IsLightTheme { get; private set; }

    public static event EventHandler? ThemeChanged;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        Preference = LoadPreference();
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        ApplyCurrentTheme();
    }

    public static void Shutdown()
    {
        if (!_initialized)
            return;

        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _initialized = false;
    }

    public static void SetPreference(ThemeMode mode)
    {
        Preference = mode;
        SavePreference(mode);
        ApplyCurrentTheme();
    }

    public static string PreferenceLabel => Preference.ToString();

    public static void ApplyWindowChromeTheme(Window window)
    {
        try
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            int useDarkMode = IsLightTheme ? 0 : 1;
            const int DwmwaUseImmersiveDarkMode = 20;
            const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

            int result = DwmSetWindowAttribute(
                handle,
                DwmwaUseImmersiveDarkMode,
                ref useDarkMode,
                Marshal.SizeOf<int>());

            if (result != 0)
            {
                DwmSetWindowAttribute(
                    handle,
                    DwmwaUseImmersiveDarkModeBefore20H1,
                    ref useDarkMode,
                    Marshal.SizeOf<int>());
            }
        }
        catch
        {
            // Native title-bar theming is best-effort. The app content still follows the selected theme.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != ThemeMode.System)
            return;

        Application.Current?.Dispatcher.BeginInvoke(new Action(ApplyCurrentTheme));
    }

    private static void ApplyCurrentTheme()
    {
        bool light = Preference switch
        {
            ThemeMode.Light => true,
            ThemeMode.Dark => false,
            _ => ReadWindowsLightTheme()
        };

        IsLightTheme = light;
        ApplyPalette(light ? LightPalette : DarkPalette);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static bool ReadWindowsLightTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            object? value = key?.GetValue("AppsUseLightTheme");
            return value is not int intValue || intValue != 0;
        }
        catch
        {
            return true;
        }
    }

    private static ThemeMode LoadPreference()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return ThemeMode.System;

            SettingsModel? settings = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(SettingsPath));
            return Enum.TryParse(settings?.Theme, ignoreCase: true, out ThemeMode mode)
                ? mode
                : ThemeMode.System;
        }
        catch
        {
            return ThemeMode.System;
        }
    }

    private static void SavePreference(ThemeMode mode)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(
                new SettingsModel { Theme = mode.ToString() },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Theme selection still applies for this session even if settings cannot be persisted.
        }
    }

    private static void ApplyPalette(IReadOnlyDictionary<string, string> palette)
    {
        Application? app = Application.Current;
        if (app is null)
            return;

        foreach ((string key, string value) in palette)
        {
            app.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
    }

    private sealed class SettingsModel
    {
        public string Theme { get; set; } = ThemeMode.System.ToString();
    }

    private static readonly IReadOnlyDictionary<string, string> DarkPalette = new Dictionary<string, string>
    {
        ["BgBrush"] = "#08111D",
        ["HeaderBrush"] = "#0B1523",
        ["PanelBrush"] = "#111D2D",
        ["PanelRaisedBrush"] = "#172538",
        ["PanelHoverBrush"] = "#1E3047",
        ["BorderBrush"] = "#334A66",
        ["BorderStrongBrush"] = "#4A6688",
        ["TextBrush"] = "#F7FAFC",
        ["MutedBrush"] = "#C3D0E0",
        ["SubtleBrush"] = "#93A8BF",
        ["AccentBrush"] = "#59E3B4",
        ["AccentHoverBrush"] = "#7CEBC6",
        ["AccentSoftBrush"] = "#143C32",
        ["AccentBorderBrush"] = "#2D725E",
        ["PrimaryButtonTextBrush"] = "#06150F",
        ["ProgressTrackBrush"] = "#263B52",
        ["FocusBrush"] = "#F7FAFC",
        ["WarningBrush"] = "#FFD166",
        ["WarningSoftBrush"] = "#493A17",
        ["DangerBrush"] = "#FF8295",
        ["DangerSoftBrush"] = "#481E28"
    };

    private static readonly IReadOnlyDictionary<string, string> LightPalette = new Dictionary<string, string>
    {
        ["BgBrush"] = "#F3F6F9",
        ["HeaderBrush"] = "#FFFFFF",
        ["PanelBrush"] = "#FFFFFF",
        ["PanelRaisedBrush"] = "#F7F9FB",
        ["PanelHoverBrush"] = "#EAF0F5",
        ["BorderBrush"] = "#D4DEE8",
        ["BorderStrongBrush"] = "#A6B5C4",
        ["TextBrush"] = "#15212E",
        ["MutedBrush"] = "#425466",
        ["SubtleBrush"] = "#65778A",
        ["AccentBrush"] = "#087A59",
        ["AccentHoverBrush"] = "#066548",
        ["AccentSoftBrush"] = "#E5F5EF",
        ["AccentBorderBrush"] = "#91CBB8",
        ["PrimaryButtonTextBrush"] = "#FFFFFF",
        ["ProgressTrackBrush"] = "#DCE5EC",
        ["FocusBrush"] = "#087A59",
        ["WarningBrush"] = "#975A00",
        ["WarningSoftBrush"] = "#FFF1CE",
        ["DangerBrush"] = "#B62F4A",
        ["DangerSoftBrush"] = "#FCE8ED"
    };
}
