using System.Diagnostics;
using System.IO;
using IOPath = System.IO.Path;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CellVera.Models;
using CellVera.Services;

namespace CellVera;

public partial class MainWindow : Window
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromHours(24);

    private readonly BatteryService _batteryService = new();
    private readonly ChargeHistoryService _historyService = new();
    private readonly BatteryNotificationService _notificationService = new();
    private readonly TrayService _trayService = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(15) };

    private bool _isRefreshing;
    private bool _themeSelectionReady;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        NotificationsCheckBox.IsChecked = _notificationService.Enabled;
        ApplySavedThemeSelection();
        _themeSelectionReady = true;

        Loaded += MainWindow_Loaded;
        SourceInitialized += (_, _) => ThemeService.ApplyWindowChromeTheme(this);
        Closed += MainWindow_Closed;
        _timer.Tick += async (_, _) => await RefreshBatteryAsync();

        ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        _trayService.ShowRequested += (_, _) => Dispatcher.Invoke(ShowFromTray);
        _trayService.RefreshRequested += (_, _) => Dispatcher.BeginInvoke(new Action(async () => await RefreshBatteryAsync()));
        _trayService.ExitRequested += (_, _) => Dispatcher.Invoke(ExitFromTray);
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

    private void ThemeService_ThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ThemeService.ApplyWindowChromeTheme(this);
            RenderHistory();
        }));
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyWindowChromeTheme(this);
        await RefreshBatteryAsync();
        _timer.Start();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _timer.Stop();
        ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
        _trayService.Dispose();
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
            BatterySnapshot battery = await Task.Run(() =>
            {
                BatterySnapshot snapshot = _batteryService.Read();
                _historyService.Record(snapshot);
                return snapshot;
            });

            ApplySnapshot(battery);
            RenderHistory();
            _trayService.Update(battery);
            _notificationService.Evaluate(battery, _trayService.ShowNotification);
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
        PowerSourceText.Text = battery.AcOnline ? "AC adapter connected" : "Running on battery";

        ApplyChargeVisualState(battery);

        if (battery.HealthPercent is double health)
        {
            HealthText.Text = $"{health:0}%";
            HealthBar.Value = Math.Clamp(health, 0, 100);
            HealthLabelText.Text = battery.HealthLabel;
            WearText.Text = $"Estimated wear {battery.WearPercent:0.0}%";
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
            WearText.Text = "Capacity data not exposed";
        }

        DesignCapacityText.Text = FormatCapacity(battery.DesignCapacityMWh);
        FullCapacityText.Text = FormatCapacity(battery.FullChargeCapacityMWh);
        CycleText.Text = battery.CycleCount?.ToString("N0") ?? "N/A";
        TemperatureText.Text = battery.TemperatureC is double temperature
            ? $"{temperature:0.0} °C"
            : "N/A";
        VoltageText.Text = battery.VoltageMv is uint millivolts
            ? $"{millivolts / 1000.0:0.00} V"
            : "N/A";
        PowerFlowText.Text = FormatPowerFlow(battery);
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

    private void RenderHistory()
    {
        if (!IsLoaded)
            return;

        IReadOnlyList<ChargeHistoryEntry> entries = _historyService.GetRecent(HistoryWindow);
        HistoryCanvas.Children.Clear();

        bool hasHistory = entries.Count > 0;
        HistoryEmptyText.Visibility = hasHistory ? Visibility.Collapsed : Visibility.Visible;
        ClearHistoryButton.IsEnabled = hasHistory;

        if (!hasHistory)
        {
            HistorySummaryText.Text = "No charge history yet";
            return;
        }

        double width = HistoryCanvas.ActualWidth;
        double height = HistoryCanvas.ActualHeight;
        if (width < 40 || height < 40)
            return;

        Brush gridBrush = GetBrush("BorderBrush");
        Brush lineBrush = GetBrush("AccentBrush");
        foreach (int percent in new[] { 25, 50, 75 })
        {
            double y = height - (percent / 100.0 * height);
            HistoryCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1,
                Opacity = 0.55
            });
        }

        DateTime end = DateTime.Now;
        DateTime start = end - HistoryWindow;
        double totalSeconds = HistoryWindow.TotalSeconds;

        var points = new PointCollection();
        foreach (ChargeHistoryEntry entry in entries)
        {
            double elapsed = Math.Clamp((entry.Timestamp - start).TotalSeconds, 0, totalSeconds);
            double x = elapsed / totalSeconds * width;
            double y = height - Math.Clamp(entry.ChargePercent, 0, 100) / 100.0 * height;
            points.Add(new Point(x, y));
        }

        if (points.Count > 1)
        {
            HistoryCanvas.Children.Add(new Polyline
            {
                Points = points,
                Stroke = lineBrush,
                StrokeThickness = 2.25,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
        }

        Point lastPoint = points[^1];
        var marker = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = lineBrush,
            Stroke = GetBrush("PanelRaisedBrush"),
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(marker, Math.Clamp(lastPoint.X - 3.5, 0, Math.Max(0, width - 7)));
        Canvas.SetTop(marker, Math.Clamp(lastPoint.Y - 3.5, 0, Math.Max(0, height - 7)));
        HistoryCanvas.Children.Add(marker);

        ChargeHistoryEntry first = entries[0];
        ChargeHistoryEntry last = entries[^1];
        int minimum = entries.Min(entry => entry.ChargePercent);
        int maximum = entries.Max(entry => entry.ChargePercent);
        string direction = last.ChargePercent > first.ChargePercent
            ? $"+{last.ChargePercent - first.ChargePercent}%"
            : $"{last.ChargePercent - first.ChargePercent}%";

        HistorySummaryText.Text = $"{entries.Count} samples · {minimum}–{maximum}% · net {direction}";

    }

    private System.Windows.Media.Brush GetBrush(string key) =>
        TryFindResource(key) as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray;

    private void HistoryCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderHistory();
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "Delete CellVera's locally stored charge history?",
            "Clear charge history",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        _historyService.Clear();
        RenderHistory();
    }

    private void NotificationsCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _notificationService.SetEnabled(NotificationsCheckBox.IsChecked == true);
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && !_isExiting)
            Hide();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void ExitFromTray()
    {
        _isExiting = true;
        Close();
    }

    private static string FormatCapacity(uint? mWh) =>
        mWh is uint value ? $"{value / 1000.0:0.0} Wh" : "N/A";

    private static string FormatPowerFlow(BatterySnapshot battery)
    {
        if (battery.ChargeRateMw is int charging)
            return $"+{Math.Abs(charging) / 1000.0:0.0} W";

        if (battery.DischargeRateMw is int discharging)
            return $"−{Math.Abs(discharging) / 1000.0:0.0} W";

        return "N/A";
    }

    private static string FormatRuntime(int? minutes, bool acOnline)
    {
        if (acOnline)
            return "On AC power";

        if (minutes is not int value || value <= 0)
            return "Runtime unavailable";

        return value >= 60 ? $"{value / 60} h {value % 60} min left" : $"{value} min left";
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

            string output = IOPath.Combine(folder, $"BatteryReport-{DateTime.Now:yyyyMMdd-HHmmss}.html");
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
