using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CellVera.Models;
using CellVera.Services;

namespace CellVera;

public partial class MainWindow : Window
{
    private readonly BatteryService _batteryService = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(15) };
    private bool _isRefreshing;
    private bool _themeSelectionReady;

    public MainWindow()
    {
        InitializeComponent();
        ApplySavedThemeSelection();
        _themeSelectionReady = true;

        Loaded += MainWindow_Loaded;
        Closed += (_, _) => _timer.Stop();
        _timer.Tick += async (_, _) => await RefreshBatteryAsync();
    }


    private void ApplySavedThemeSelection()
    {
        switch (ThemeService.Preference)
        {
            case ThemeMode.Light:
                LightThemeRadio.IsChecked = true;
                break;
            case ThemeMode.Dark:
                DarkThemeRadio.IsChecked = true;
                break;
            default:
                SystemThemeRadio.IsChecked = true;
                break;
        }
    }

    private void ThemeMode_Checked(object sender, RoutedEventArgs e)
    {
        if (!_themeSelectionReady || sender is not RadioButton { Tag: string tag })
            return;

        if (Enum.TryParse(tag, ignoreCase: true, out ThemeMode mode))
            ThemeService.SetPreference(mode);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshBatteryAsync();
        _timer.Start();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBatteryAsync();
    }

    private async Task RefreshBatteryAsync()
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;
        RefreshButton.IsEnabled = false;
        RefreshButton.Content = "Refreshing…";

        try
        {
            BatterySnapshot battery = await Task.Run(_batteryService.Read);
            ApplySnapshot(battery);
            LastUpdatedText.Text = $"Updated {DateTime.Now:h:mm:ss tt}";
        }
        catch (Exception ex)
        {
            LastUpdatedText.Text = "Battery data unavailable";
            MessageBox.Show(
                $"CellVera could not read battery information.\n\n{ex.Message}",
                "CellVera",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            RefreshButton.Content = "Refresh";
            RefreshButton.IsEnabled = true;
            _isRefreshing = false;
        }
    }

    private void ApplySnapshot(BatterySnapshot battery)
    {
        ChargeText.Text = $"{battery.ChargePercent}%";
        ChargeProgress.Value = battery.ChargePercent;
        BatteryNameText.Text = battery.Name;
        StateText.Text = battery.State;
        RuntimeText.Text = FormatRuntime(battery.EstimatedRuntimeMinutes, battery.AcOnline);
        PowerSourceText.Text = battery.AcOnline ? "AC adapter" : "Battery";

        ApplyChargeVisualState(battery);

        if (battery.HealthPercent is double health)
        {
            HealthText.Text = $"{health:0}%";
            HealthBar.Value = Math.Clamp(health, 0, 100);
            HealthLabelText.Text = battery.HealthLabel;
            WearText.Text = $"Estimated wear: {battery.WearPercent:0.0}%";
            HealthBar.SetResourceReference(
                Control.ForegroundProperty,
                health switch
                {
                    >= 80 => "AccentBrush",
                    >= 65 => "WarningBrush",
                    _ => "DangerBrush"
                });
        }
        else
        {
            HealthText.Text = "—";
            HealthBar.Value = 0;
            HealthBar.SetResourceReference(Control.ForegroundProperty, "BorderStrongBrush");
            HealthLabelText.Text = "Not available";
            WearText.Text = "Capacity data is not exposed by this battery driver.";
        }

        DesignCapacityText.Text = FormatCapacity(battery.DesignCapacityMWh);
        FullCapacityText.Text = FormatCapacity(battery.FullChargeCapacityMWh);
        CycleText.Text = battery.CycleCount?.ToString("N0") ?? "Not available";
        TemperatureText.Text = battery.TemperatureC is double temperature
            ? $"{temperature:0.0} °C"
            : "Not available";
        VoltageText.Text = battery.VoltageMv is uint millivolts
            ? $"{millivolts / 1000.0:0.00} V"
            : "Not available";
        ChargeRateText.Text = FormatRate(battery.ChargeRateMw);
        DischargeRateText.Text = FormatRate(battery.DischargeRateMw);
    }

    private void ApplyChargeVisualState(BatterySnapshot battery)
    {
        if (battery.AcOnline)
        {
            ChargeProgress.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            StateBadge.SetResourceReference(Border.BackgroundProperty, "AccentSoftBrush");
            StateText.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
            return;
        }

        if (battery.ChargePercent <= 15)
        {
            ChargeProgress.SetResourceReference(Control.ForegroundProperty, "DangerBrush");
            StateBadge.SetResourceReference(Border.BackgroundProperty, "DangerSoftBrush");
            StateText.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
        }
        else if (battery.ChargePercent <= 30)
        {
            ChargeProgress.SetResourceReference(Control.ForegroundProperty, "WarningBrush");
            StateBadge.SetResourceReference(Border.BackgroundProperty, "WarningSoftBrush");
            StateText.SetResourceReference(TextBlock.ForegroundProperty, "WarningBrush");
        }
        else
        {
            ChargeProgress.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            StateBadge.SetResourceReference(Border.BackgroundProperty, "AccentSoftBrush");
            StateText.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        }
    }


    private static string FormatCapacity(uint? mWh) =>
        mWh is uint value ? $"{value / 1000.0:0.0} Wh" : "Not available";

    private static string FormatRate(int? mW) =>
        mW is int value ? $"{Math.Abs(value) / 1000.0:0.0} W" : "Not available";

    private static string FormatRuntime(int? minutes, bool acOnline)
    {
        if (acOnline)
            return "Connected to AC";

        if (minutes is not int value || value <= 0)
            return "Not available";

        return value >= 60 ? $"{value / 60} h {value % 60} min" : $"{value} min";
    }

    private void PowerSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:powersleep") { UseShellExecute = true });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.PowerOptions")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Windows power settings could not be opened.\n\n{ex.Message}",
                    "Power settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string output = Path.Combine(folder, $"BatteryReport-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/batteryreport /output \"{output}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
                throw new InvalidOperationException("Windows could not start powercfg.exe.");

            bool finished = process.WaitForExit(20000);
            if (!finished)
            {
                try { process.Kill(true); } catch { }

                MessageBox.Show(
                    "Windows battery report generation timed out. Try again, or run 'powercfg /batteryreport' from an Administrator Terminal.",
                    "Battery report",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (process.ExitCode == 0 && File.Exists(output))
            {
                Process.Start(new ProcessStartInfo(output) { UseShellExecute = true });
                LastUpdatedText.Text = "Battery report created";
                return;
            }

            string error = process.StandardError.ReadToEnd().Trim();
            if (string.IsNullOrWhiteSpace(error))
                error = process.StandardOutput.ReadToEnd().Trim();

            MessageBox.Show(
                string.IsNullOrWhiteSpace(error)
                    ? "Windows did not create the battery report. Try running CellVera as administrator."
                    : $"Windows did not create the battery report.\n\n{error}",
                "Battery report",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The battery report could not be generated.\n\n{ex.Message}",
                "Battery report",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
