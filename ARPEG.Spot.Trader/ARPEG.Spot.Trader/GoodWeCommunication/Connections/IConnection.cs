namespace ARPEG.Spot.Trader.GoodWeCommunication.Connections;

public interface IConnection
{
    public Task<byte[]> Send(byte[] message,
        CancellationToken cancellationToken);
}