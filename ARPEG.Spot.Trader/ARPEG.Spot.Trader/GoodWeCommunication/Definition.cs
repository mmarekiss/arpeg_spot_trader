using ARPEG.Spot.Trader.GoodWeCommunication.Connections;
using ARPEG.Spot.Trader.Integration;

namespace ARPEG.Spot.Trader.GoodWeCommunication;

public record Definition(string Sn,
    LicenceVersion Licence,
    IConnection Connection);
