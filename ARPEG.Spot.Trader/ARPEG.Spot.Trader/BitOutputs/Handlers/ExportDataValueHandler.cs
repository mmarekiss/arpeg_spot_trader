using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public class ExportDataValueHandler : IDataValueHandler
{
    private readonly ILogger<ExportDataValueHandler> _logger;

    public ExportDataValueHandler(ILogger<ExportDataValueHandler> logger)
    {
        _logger = logger;
    }

    public string Type => "Export";

    public bool? Handle(DataValue value,
        BitOutputOptions options)
    {
        if (value is { group: DataGroupNames.Grid, part: GridGroupParts.Total })
        {
            if (options.GreaterThen == (value.value > options.TriggerValue))
                return true;
            if (options.GreaterThenOff == (value.value > options.TriggerValueOff))
                return false;
        }

        return null;
    }

}