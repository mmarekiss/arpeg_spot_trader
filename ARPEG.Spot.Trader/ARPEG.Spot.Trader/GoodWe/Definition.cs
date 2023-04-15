using System.Net;
using ARPEG.Spot.Trader.Integration;

namespace TecoBridge.GoodWe;

public class Definition
{
    public required IPAddress Address { get; init; }
    public required string SN { get;  init;}

    public LicenceVersion Licence { get; init; }
}