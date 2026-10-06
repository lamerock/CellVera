using System.IO;
using System.Text.Json;
using CellVera.Models;

namespace CellVera.Services;

public sealed class BatteryNotificationService
{
    private readonly string _settingsPath;
    private bool _lowShown;
    private bool _criticalShown;
    private bool _fullShown;
    private bool _initialized;

    public BatteryNotificationService()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CellVera");

        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "notifications.json");
        Enabled = LoadEnabled();
    }

    public bool Enabled { get; private set; } = true;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        SaveEnabled();
    }

    public void Evaluate(BatterySnapshot snapshot, Action<string, string, NotificationKind> notify)
    {
        if (!Enabled)
            return;

        bool currentlyFull = snapshot.AcOnline
            && snapshot.ChargePercent >= 99
            && (snapshot.State.Contains("full", StringComparison.OrdinalIgnoreCase)
                || snapshot.ChargePercent == 100);

        if (!_initialized)
        {
            // Do not announce an already-full battery every time CellVera starts.
            _fullShown = currentlyFull;
            _initialized = true;
        }

        if (snapshot.ChargePercent > 25)
        {
            _lowShown = false;
            _criticalShown = false;
        }

        if (!snapshot.AcOnline && snapshot.ChargePercent <= 10)
        {
            if (!_criticalShown)
            {
                notify(
                    "Battery critically low",
                    $"CellVera reports {snapshot.ChargePercent}% remaining. Connect the charger soon.",
                    NotificationKind.Warning);
                _criticalShown = true;
                _lowShown = true;
            }
        }
        else if (!snapshot.AcOnline && snapshot.ChargePercent <= 20 && !_lowShown)
        {
            notify(
                "Battery low",
                $"CellVera reports {snapshot.ChargePercent}% remaining.",
                NotificationKind.Warning);
            _lowShown = true;
        }

        if (currentlyFull && !_fullShown)
        {
            notify(
                "Battery fully charged",
                "CellVera reports that the battery is fully charged.",
                NotificationKind.Info);
            _fullShown = true;
        }
        else if (!snapshot.AcOnline || snapshot.ChargePercent < 95)
        {
            _fullShown = false;
        }
    }

    private bool LoadEnabled()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return true;

            SettingsModel? settings = JsonSerializer.Deserialize<SettingsModel>(File.ReadAllText(_settingsPath));
            return settings?.Enabled ?? true;
        }
        catch
        {
            return true;
        }
    }

    private void SaveEnabled()
    {
        try
        {
            string json = JsonSerializer.Serialize(
                new SettingsModel { Enabled = Enabled },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Keep the user's in-memory preference even if persistence fails.
        }
    }

    private sealed class SettingsModel
    {
        public bool Enabled { get; set; } = true;
    }
}

public enum NotificationKind
{
    Info,
    Warning
}
