using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.GoodWeCommunication;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs.Handlers;

public interface IDataValueHandler
{
    string Type { get; }

    string Unit { get; }

    bool? Handle(DataValue value,
        BitOutputOptions options);
}