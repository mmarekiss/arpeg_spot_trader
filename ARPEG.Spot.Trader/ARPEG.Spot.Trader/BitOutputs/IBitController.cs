using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs;

public interface IBitController
{
    Task HandleDataValue(Definition inverterDefinition,
        DataValue dataValue);
}