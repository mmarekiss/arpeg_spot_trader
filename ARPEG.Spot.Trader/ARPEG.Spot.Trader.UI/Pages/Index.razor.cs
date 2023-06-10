using ARPEG.Spot.Trader.BitOutputs.Handlers;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Integration;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using Microsoft.AspNetCore.Components;

namespace ARPEG.Spot.Trader.UI.Pages;

public partial class Index
{
    [Inject]
    public IGoodWeInvStore GwStore { get; init; } = null!;

    [Inject]
    public  IConfigUpdater  ConfigUpdater { get; init; } = null!;
    
    [Inject]
    public  PriceService PriceService { get; init; } = null!;
    
    [Inject]
    public  NowManualBatteryService NowManualBatteryService { get; init; } = null!;

    [Inject]
    public  IEnumerable<IDataValueHandler> BitHandlers { get; init; } = null!;
    private Root? Configuration { get; set; }

    private double CurrentPrice { get; set; }

    private NowManualBatteryService.BatteryChargeDirection CurrentDirection { get; set; }

    protected override Task OnInitializedAsync()
    {
        Configuration = ConfigUpdater?.GetCurrent();
        CurrentPrice = PriceService.GetCurrentPrice();
        CurrentDirection = NowManualBatteryService.GetActiveDirection();
        return base.OnInitializedAsync();
    }
    
    private async void Save()
    {
        if (Configuration is not null)
        {
            await ConfigUpdater.SaveCurrent(Configuration, CancellationToken.None);
        }
    }

    private void SetManualBattery(NowManualBatteryService.BatteryChargeDirection action)
    {
        NowManualBatteryService.SetBatteryMode(action);
        CurrentDirection = NowManualBatteryService.GetActiveDirection();
    }
}