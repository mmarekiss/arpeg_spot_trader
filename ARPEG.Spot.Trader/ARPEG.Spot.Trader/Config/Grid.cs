using System.ComponentModel.DataAnnotations;
using ARPEG.Spot.Trader.Config.BitOutputs;

namespace ARPEG.Spot.Trader.Config;

public class Grid
{
    public bool TradeEnergy { get; set; }

    [Range(0,10000)]
    public int ChargePower { get; set; } = 4000;

    [Range(0,10000)]
    public int? ExportLimit { get; set; }

}