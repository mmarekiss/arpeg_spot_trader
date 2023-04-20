namespace ARPEG.Spot.Trader.Constants;

public static class DataGroupNames
{
    public const string Battery = nameof(Battery);
    public const string PV = nameof(PV);
    public const string Grid = nameof(Grid);
    public const string Temperature = nameof(Temperature);
}

public static class TemperatureGroupParts
{
    public const string Temperature_Air = nameof(Temperature_Air);
}

public static class BatteryGroupParts
{
    public const string SOC = nameof(SOC);
}

public static class GridGroupParts
{
    public const string Total = nameof(Total);
}

public static class PVGroupParts
{
    public const string PV1_Wats = nameof(PV1_Wats);
    public const string PV2_Wats = nameof(PV2_Wats);
    public const string Wats = nameof(Wats);
}