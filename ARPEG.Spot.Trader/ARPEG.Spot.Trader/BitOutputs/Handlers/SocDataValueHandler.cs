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
        bool greater,
        short limit)
    {
        if(value is { group: DataGroupNames.Battery, part: BatteryGroupParts.SOC })
                  return greater == (value.value > limit);
        return null;
    }

}