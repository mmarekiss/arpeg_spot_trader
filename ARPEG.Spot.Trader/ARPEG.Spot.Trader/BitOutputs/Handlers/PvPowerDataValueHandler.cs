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
        bool greater,
        short limit)
    {
        if(value is { group: DataGroupNames.PV, part: PVGroupParts.Wats })
                  return greater == (value.value > limit);
        return null;
    }

}