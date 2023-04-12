using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Store;

public class GoodWeInvStore
{
    private List<GoodWeCom> _gws = new();

    public IEnumerable<GoodWeCom> GoodWes => _gws; 

    public void AddGoodWe(GoodWeCom gw)
    {
        _gws.Add(gw);
    }
}