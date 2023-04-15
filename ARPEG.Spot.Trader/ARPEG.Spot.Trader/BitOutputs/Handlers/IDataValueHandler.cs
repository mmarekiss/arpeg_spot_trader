using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public interface IDataValueHandler
{
    string Type { get; }

    bool? Handle(DataValue value,
        bool greater,
        short limit);
}