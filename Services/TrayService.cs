using System.Drawing;
using Forms = System.Windows.Forms;
using CellVera.Models;

namespace CellVera.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Icon? _icon;

    public TrayService()
    {
        _icon = TryLoadApplicationIcon();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open CellVera", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Refresh battery", null, (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "CellVera",
            Visible = true,
            ContextMenuStrip = menu,
            Icon = _icon ?? SystemIcons.Application
        };

        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ShowRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? ExitRequested;

    public void Update(BatterySnapshot snapshot)
    {
        string source = snapshot.AcOnline ? "AC" : "Battery";
        string text = $"CellVera · {snapshot.ChargePercent}% · {source}";
        _notifyIcon.Text = text.Length <= 63 ? text : text[..63];
    }

    public void ShowNotification(string title, string message, NotificationKind kind)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = kind == NotificationKind.Warning
            ? Forms.ToolTipIcon.Warning
            : Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }

    private static Icon? TryLoadApplicationIcon()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            return string.IsNullOrWhiteSpace(processPath)
                ? null
                : Icon.ExtractAssociatedIcon(processPath);
        }
        catch
        {
            return null;
        }
    }
}
