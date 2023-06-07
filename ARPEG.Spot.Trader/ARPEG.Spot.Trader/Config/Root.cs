using ARPEG.Spot.Trader.Config.BitOutputs;

namespace ARPEG.Spot.Trader.Config;

public class Root
{
    public PvForecast PvForecast { get; init; } = new();

    public ManualBatteryConfig ManualBatteryConfig { get; init; } = new ManualBatteryConfig();

    public BitOutput1 BitOutput1 { get; init; } = new();

    public BitOutput2 BitOutput2 { get; init; } = new();

    public BitOutput3 BitOutput3 { get; init; } = new();

    public BitOutput4 BitOutput4 { get; init; } = new();

    public BitOutput5 BitOutput5 { get; init; } = new();

    public BitOutput6 BitOutput6 { get; init; } = new();

    public BitOutput7 BitOutput7 { get; init; } = new();

    public BitOutput8 BitOutput8 { get; init; } = new();
}