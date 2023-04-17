using ARPEG.Spot.Trader.Config.BitOutputs;

namespace ARPEG.Spot.Trader.Config;

public class Root
{
    public GoodWe GoodWe { get; init; } = new();

    public Grid Grid { get; init; } = new();

    public PvForecast PvForecast { get; init; } = new();

    public BitOutput1 BitOutput1 { get; init; } = new();

    public BitOutput1 BitOutput2 { get; init; } = new();

    public BitOutput1 BitOutput3 { get; init; } = new();

    public BitOutput1 BitOutput4 { get; init; } = new();

    public BitOutput1 BitOutput5 { get; init; } = new();

    public BitOutput1 BitOutput6 { get; init; } = new();

    public BitOutput1 BitOutput7 { get; init; } = new();

    public BitOutput1 BitOutput8 { get; init; } = new();
}