using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using ARPEG.Spot.Trader.GoodWeCommunication;
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
    
    public string Unit => "W";

    public bool? Handle(DataValue value,
        BitOutputOptions options)
    {
        if (value is { group: DataGroupNames.Grid, part: GridGroupParts.Total })
        {
            if (options.GreaterThen == (value.value < options.TriggerValueOff))
                return false;
            if (options.GreaterThen == (value.value > options.TriggerValue))
                return true;
        }

        return null;
    }

}