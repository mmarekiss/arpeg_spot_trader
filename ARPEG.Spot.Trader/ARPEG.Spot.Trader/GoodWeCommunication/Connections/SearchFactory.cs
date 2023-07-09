using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ARPEG.Spot.Trader.GoodWeCommunication.Connections;

public class SearchFactory
{
    public async Task<IPAddress?> TryFetchBroadcastAsync(IPAddress broadcast, CancellationToken ct)
    {
        var client = new UdpClient();
        try
        {
            await client.SendAsync(Encoding.ASCII.GetBytes("WIFIKIT-214028-READ"), new IPEndPoint(broadcast, 48899), ct);
            var response = client.ReceiveAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            if (response is { IsCompleted: true, Result.Buffer.Length: > 0 })
            {
                var result = Encoding.ASCII.GetString(response.Result.Buffer);
                if (IPAddress.TryParse(result.Split(',').FirstOrDefault(), out var inverter))
                {
                    return inverter;
                }
            }
        }
        finally
        {
            client.Dispose();
        }

        return null;
    }
}