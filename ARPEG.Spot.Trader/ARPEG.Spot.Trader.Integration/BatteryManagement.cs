namespace ARPEG.Spot.Trader.Integration;

public enum BatteryManagement
{
    Normal,
    ForceCharge,
    ForceDischarge
}

public class BatteryManagementCommands
{
    public string SerialNumber { get; init; } = string.Empty;
    public BatteryManagement BatteryManagement { get; init; }
}