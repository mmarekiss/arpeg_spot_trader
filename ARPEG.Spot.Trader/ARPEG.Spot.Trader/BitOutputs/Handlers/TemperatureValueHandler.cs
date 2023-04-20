using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public class TemperatureValueHandler : IDataValueHandler
{
    public string Type => "Teplota";

    public string Unit => "°C";

    public bool? Handle(DataValue value,
        BitOutputOptions options)
    {
        if(value is { group: DataGroupNames.Battery, part: TemperatureGroupParts.Temperature_Air })
        {
            if (options.GreaterThen == (value.value < options.TriggerValueOff))
                return false;
            if (options.GreaterThen == (value.value > options.TriggerValue))
                return true;
        }
        return null;
    }
}