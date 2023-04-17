using ARPEG.Spot.Trader.BitOutputs.Handlers;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using Microsoft.AspNetCore.Components;

namespace ARPEG.Spot.Trader.UI.Pages;

public partial class Index
{
    [Inject]
    public required IGoodWeInvStore GwStore { get; init; }

    [Inject]
    public required  IConfigUpdater  ConfigUpdater { get; init; }

    [Inject]
    public required  IEnumerable<IDataValueHandler> BitHandlers { get; init; }
    private Root? Configuration { get; set; } 
    
    protected override Task OnInitializedAsync()
    {
        Configuration = ConfigUpdater.GetCurrent();
        return base.OnInitializedAsync();
    }

    private async void Save()
    {
        if (Configuration is not null)
        {
            await ConfigUpdater.SaveCurrent(Configuration, CancellationToken.None);
        }
    }
}