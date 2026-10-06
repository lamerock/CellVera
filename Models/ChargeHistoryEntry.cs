namespace CellVera.Models;

public sealed class ChargeHistoryEntry
{
    public DateTime Timestamp { get; init; }
    public int ChargePercent { get; init; }
    public bool AcOnline { get; init; }
    public string State { get; init; } = "Unknown";
}
