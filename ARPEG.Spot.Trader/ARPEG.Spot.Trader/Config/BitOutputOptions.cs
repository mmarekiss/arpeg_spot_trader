using System.Device.Gpio;

namespace ARPEG.Spot.Trader.Config;

public abstract class BitOutputOptions
{
    public abstract int Pin { get;  }
    
    public required string GwSn { get; set; }

    public required string DriverType { get; set; } 

    public short TriggerValue { get; set; }

    public bool GreaterThen { get; set; }
}