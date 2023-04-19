using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public class SocDataValueHandler : IDataValueHandler
{
    private readonly ILogger<SocDataValueHandler> _logger;

    public SocDataValueHandler(ILogger<SocDataValueHandler> logger)
    {
        _logger = logger;
    }

    public string Type => "SOC";

    public bool? Handle(DataValue value,
        BitOutputOptions options)
    {
        if(value is { group: DataGroupNames.Battery, part: BatteryGroupParts.SOC })
        {
            if (options.GreaterThenOff == (value.value > options.TriggerValueOff))
                return false;
            if (options.GreaterThen == (value.value > options.TriggerValue))
                return true;
        }
        return null;
    }

}