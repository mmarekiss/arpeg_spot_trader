namespace ARPEG.Spot.Trader.Integration;

public enum LicenceVersion
{
    None,
    Standard
}

public class GetLicence
{
    public required string SerialNumber { get; init; }
    public required LicenceVersion LicenceVersion { get; init; }
}
