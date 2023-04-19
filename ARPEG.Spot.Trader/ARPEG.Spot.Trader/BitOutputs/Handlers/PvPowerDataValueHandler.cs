using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public class PvPowerDataValueHandler : IDataValueHandler
{
    private readonly ILogger<PvPowerDataValueHandler> _logger;

    public PvPowerDataValueHandler(ILogger<PvPowerDataValueHandler> logger)
    {
        _logger = logger;
    }

    public string Type => "PV_Power";

    public bool? Handle(DataValue value,
        BitOutputOptions options)
    {
        if (value is { group: DataGroupNames.PV, part: PVGroupParts.Wats })
        {
            if (options.GreaterThenOff == (value.value > options.TriggerValueOff))
                return false;
            if (options.GreaterThen == (value.value > options.TriggerValue))
                return true;
        }

        return null;
    }

}