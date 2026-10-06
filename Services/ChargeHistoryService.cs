using System.IO;
using System.Text.Json;
using CellVera.Models;

namespace CellVera.Services;

public sealed class ChargeHistoryService
{
    private static readonly TimeSpan MinimumSampleInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);
    private const int MaximumEntries = 5000;

    private readonly string _historyPath;
    private readonly object _sync = new();
    private List<ChargeHistoryEntry> _entries;

    public ChargeHistoryService()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CellVera");

        Directory.CreateDirectory(directory);
        _historyPath = Path.Combine(directory, "charge-history.json");
        _entries = Load();
        Prune(DateTime.Now);
    }

    public IReadOnlyList<ChargeHistoryEntry> GetRecent(TimeSpan window)
    {
        lock (_sync)
        {
            DateTime cutoff = DateTime.Now - window;
            return _entries
                .Where(entry => entry.Timestamp >= cutoff)
                .OrderBy(entry => entry.Timestamp)
                .ToList();
        }
    }

    public void Record(BatterySnapshot snapshot)
    {
        lock (_sync)
        {
            DateTime now = DateTime.Now;
            ChargeHistoryEntry? last = _entries.LastOrDefault();

            bool shouldRecord = last is null
                || Math.Abs(last.ChargePercent - snapshot.ChargePercent) >= 1
                || last.AcOnline != snapshot.AcOnline
                || !string.Equals(last.State, snapshot.State, StringComparison.OrdinalIgnoreCase)
                || now - last.Timestamp >= MinimumSampleInterval;

            if (!shouldRecord)
                return;

            _entries.Add(new ChargeHistoryEntry
            {
                Timestamp = now,
                ChargePercent = snapshot.ChargePercent,
                AcOnline = snapshot.AcOnline,
                State = snapshot.State
            });

            Prune(now);
            Save();
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
            Save();
        }
    }

    private List<ChargeHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(_historyPath))
                return new List<ChargeHistoryEntry>();

            string json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<List<ChargeHistoryEntry>>(json)
                   ?? new List<ChargeHistoryEntry>();
        }
        catch
        {
            return new List<ChargeHistoryEntry>();
        }
    }

    private void Prune(DateTime now)
    {
        DateTime cutoff = now - Retention;
        _entries = _entries
            .Where(entry => entry.Timestamp >= cutoff)
            .OrderBy(entry => entry.Timestamp)
            .TakeLast(MaximumEntries)
            .ToList();
    }

    private void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(_historyPath)!;
            Directory.CreateDirectory(directory);

            string tempPath = _historyPath + ".tmp";
            string json = JsonSerializer.Serialize(
                _entries,
                new JsonSerializerOptions { WriteIndented = false });

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _historyPath, overwrite: true);
        }
        catch
        {
            // History is a convenience feature. Battery monitoring should keep working
            // even if the local history file cannot be written.
        }
    }
}
