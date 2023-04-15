namespace ARPEG.Spot.Trader.Config;

public class Grid
{
    public bool TradeEnergy { get; set; }

    public ushort ChargePower { get; set; } = 4000;

    public ushort? ExportLimit { get; set; }
}