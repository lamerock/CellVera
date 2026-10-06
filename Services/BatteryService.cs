using System.Management;
using System.Runtime.InteropServices;
using CellVera.Models;

namespace CellVera.Services;

public sealed class BatteryService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);

    public BatterySnapshot Read()
    {
        string name = "Battery";
        int charge = 0;
        string state = "Unknown";
        bool acOnline = false;
        uint? design = null;
        uint? full = null;
        uint? remaining = null;
        uint? voltage = null;
        int? chargeRate = null;
        int? dischargeRate = null;
        uint? cycles = null;
        double? temperature = null;
        int? runtime = null;

        if (GetSystemPowerStatus(out var ps))
        {
            acOnline = ps.ACLineStatus == 1;
            if (ps.BatteryLifePercent <= 100)
                charge = ps.BatteryLifePercent;

            if (ps.BatteryLifeTime != uint.MaxValue)
                runtime = checked((int)(ps.BatteryLifeTime / 60));
        }

        TryReadCimv2(ref name, ref charge, ref state, ref design, ref full, ref runtime);

        // Root\WMI often exposes richer firmware/ACPI battery details than Win32_Battery.
        design ??= QueryUInt32(@"root\wmi", "BatteryStaticData", "DesignedCapacity");
        full ??= QueryUInt32(@"root\wmi", "BatteryFullChargedCapacity", "FullChargedCapacity");
        cycles ??= QueryUInt32(@"root\wmi", "BatteryCycleCount", "CycleCount");
        remaining ??= QueryUInt32(@"root\wmi", "BatteryStatus", "RemainingCapacity");
        voltage ??= QueryUInt32(@"root\wmi", "BatteryStatus", "Voltage");
        chargeRate ??= QueryInt32(@"root\wmi", "BatteryStatus", "ChargeRate");
        dischargeRate ??= QueryInt32(@"root\wmi", "BatteryStatus", "DischargeRate");

        // Not every battery driver publishes this class/property; failures are expected.
        var rawTemp = QueryUInt32(@"root\wmi", "BatteryTemperature", "CurrentTemperature");
        if (rawTemp is > 0)
        {
            // ACPI battery temperature commonly uses tenths of Kelvin.
            var c = rawTemp.Value / 10.0 - 273.15;
            if (c is > -30 and < 120)
                temperature = c;
        }

        if (string.Equals(state, "Unknown", StringComparison.OrdinalIgnoreCase))
            state = acOnline ? (charge >= 100 ? "Fully charged" : "Plugged in") : "On battery";

        return new BatterySnapshot
        {
            Name = name,
            ChargePercent = Math.Clamp(charge, 0, 100),
            State = state,
            AcOnline = acOnline,
            RemainingCapacityMWh = NormalizeCapacity(remaining),
            DesignCapacityMWh = NormalizeCapacity(design),
            FullChargeCapacityMWh = NormalizeCapacity(full),
            VoltageMv = NormalizeValue(voltage),
            ChargeRateMw = NormalizeRate(chargeRate),
            DischargeRateMw = NormalizeRate(dischargeRate),
            CycleCount = NormalizeValue(cycles),
            TemperatureC = temperature,
            EstimatedRuntimeMinutes = runtime is > 0 and < 100000 ? runtime : null
        };
    }

    private static void TryReadCimv2(
        ref string name,
        ref int charge,
        ref string state,
        ref uint? design,
        ref uint? full,
        ref int? runtime)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM Win32_Battery");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                name = ReadString(obj, "Name") ?? name;
                charge = ReadInt(obj, "EstimatedChargeRemaining") ?? charge;
                design = ReadUInt(obj, "DesignCapacity") ?? design;
                full = ReadUInt(obj, "FullChargeCapacity") ?? full;
                var estimatedRuntime = ReadInt(obj, "EstimatedRunTime");
                if (estimatedRuntime is int r && r > 0 && r < 100000)
                    runtime = r;

                var code = ReadInt(obj, "BatteryStatus");
                state = code switch
                {
                    1 => "Discharging",
                    2 => "On AC power",
                    3 => "Fully charged",
                    4 => "Low",
                    5 => "Critical",
                    6 => "Charging",
                    7 => "Charging · high",
                    8 => "Charging · low",
                    9 => "Charging · critical",
                    11 => "Partially charged",
                    _ => state
                };
                break;
            }
        }
        catch
        {
            // The dashboard degrades gracefully if a WMI provider is unavailable.
        }
    }

    private static uint? QueryUInt32(string scope, string className, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT {property} FROM {className}");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
                return ReadUInt(obj, property);
        }
        catch { }
        return null;
    }

    private static int? QueryInt32(string scope, string className, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT {property} FROM {className}");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
                return ReadInt(obj, property);
        }
        catch { }
        return null;
    }

    private static uint? ReadUInt(ManagementObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToUInt32(v);
        }
        catch { return null; }
    }

    private static int? ReadInt(ManagementObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToInt32(v);
        }
        catch { return null; }
    }

    private static string? ReadString(ManagementObject obj, string property)
    {
        try { return obj[property]?.ToString(); }
        catch { return null; }
    }

    private static uint? NormalizeCapacity(uint? value) => value is > 0 and < 10_000_000 ? value : null;
    private static uint? NormalizeValue(uint? value) => value is > 0 and < 10_000_000 ? value : null;
    private static int? NormalizeRate(int? value) => value is > -10_000_000 and < 10_000_000 && value != 0 ? value : null;
}
