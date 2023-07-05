namespace ARPEG.Spot.Trader.GoodWeCommunication;

public record DataPoint(string Group, string Description,
    ushort Address, int? Min = null, int? Max = null);