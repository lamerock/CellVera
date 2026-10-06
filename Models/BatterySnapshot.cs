namespace CellVera.Models;

public sealed class BatterySnapshot
{
    public string Name { get; init; } = "Battery";
    public int ChargePercent { get; init; }
    public string State { get; init; } = "Unknown";
    public bool AcOnline { get; init; }
    public uint? RemainingCapacityMWh { get; init; }
    public uint? DesignCapacityMWh { get; init; }
    public uint? FullChargeCapacityMWh { get; init; }
    public uint? VoltageMv { get; init; }
    public int? ChargeRateMw { get; init; }
    public int? DischargeRateMw { get; init; }
    public uint? CycleCount { get; init; }
    public double? TemperatureC { get; init; }
    public int? EstimatedRuntimeMinutes { get; init; }

    public double? HealthPercent =>
        DesignCapacityMWh is > 0 && FullChargeCapacityMWh is > 0
            ? Math.Clamp(FullChargeCapacityMWh.Value * 100.0 / DesignCapacityMWh.Value, 0, 150)
            : null;

    public double? WearPercent => HealthPercent is null ? null : Math.Max(0, 100 - HealthPercent.Value);

    public string HealthLabel => HealthPercent switch
    {
        null => "Unknown",
        >= 90 => "Excellent",
        >= 80 => "Good",
        >= 65 => "Fair",
        _ => "Service recommended"
    };
}
