using System.Net;
using ARPEG.Spot.Trader.Integration;

namespace TecoBridge.GoodWe;

public class Definition
{
    public IPAddress Address { get; init; } = IPAddress.None;

    public string SN { get; init; } = string.Empty;

    public LicenceVersion Licence { get; init; }
}