namespace ARPEG.Spot.Trader.GoodWeCommunication;

public record DataValue(ushort address,
    string group,
    string part,
    short value);