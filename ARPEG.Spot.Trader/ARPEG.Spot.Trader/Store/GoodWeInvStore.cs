using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Store;

public class GoodWeInvStore : IGoodWeInvStore
{
    private List<Definition> _gws = new();

    public IEnumerable<Definition> GoodWes => _gws; 

    public void AddGoodWe(Definition gw)
    {
        _gws.Add(gw);
    }
}