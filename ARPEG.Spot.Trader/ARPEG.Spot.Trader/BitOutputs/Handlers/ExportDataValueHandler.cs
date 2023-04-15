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
        bool greater,
        short limit)
    {
        if(value is { group: DataGroupNames.Grid, part: GridGroupParts.Total })
                  return greater == (value.value > limit);
        return null;
    }

}