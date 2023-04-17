using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Store;

public interface IGoodWeInvStore
{
    IEnumerable<Definition> GoodWes { get; }

    void AddGoodWe(Definition gw);
}