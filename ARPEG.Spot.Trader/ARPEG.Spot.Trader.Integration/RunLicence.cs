namespace ARPEG.Spot.Trader.Integration;

[Flags]
public enum LicenceVersion
{
    None = 0x00,
    Standard = 0x01
}

public class RunLicence
{
    public required string SerialNumber { get; init; }
    public required LicenceVersion LicenceVersion { get; init; }
}
