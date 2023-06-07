namespace ARPEG.Spot.Trader.Config;

public class ManualBatteryConfig
{
    public bool Charge { get; set; }

    public int MaxPriceForCharge { get; set; }

    public bool Discharge { get; set; }

    public int MinPriceForDischarge { get; set; }
}