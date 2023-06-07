namespace ARPEG.Spot.Trader.Integration;

[Flags]
public enum LicenceVersion
{
    None = 0x00,
    Standard = 0x01,
    Spot = 0x02,
    AvoidNegativePrice = 0x04,
    Outputs = 0x08,
    ManualBattery = 0x10
}

public class RunLicence
{
    public string SerialNumber { get; init; } = string.Empty;
    public LicenceVersion LicenceVersion { get; init; }
}
