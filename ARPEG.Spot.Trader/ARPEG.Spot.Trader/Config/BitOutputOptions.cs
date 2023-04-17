using System.Device.Gpio;

namespace ARPEG.Spot.Trader.Config;

public abstract class BitOutputOptions
{
    public abstract int Pin { get;  }
    
    public string GwSn { get; set; } = string.Empty;

    public string DriverType { get; set; }= string.Empty; 

    public short TriggerValue { get; set; }

    public bool GreaterThen { get; set; }
}