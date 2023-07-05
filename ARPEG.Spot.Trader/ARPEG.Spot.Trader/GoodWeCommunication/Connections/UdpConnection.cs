using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader.GoodWeCommunication.Connections;

public class UdpConnection : IConnection
{
    private readonly ILogger<UdpConnection> logger;

    public UdpConnection(ILogger<UdpConnection> logger)
    {
        this.logger = logger;
    }

    private IPAddress IpAddress { get; set; } = IPAddress.None;

    public async Task<byte[]> Send(byte[] message, CancellationToken cancellationToken)
    {
        using var client = GetClient(IpAddress);
        await client.SendAsync(message, cancellationToken);


        var response = client.ReceiveAsync(cancellationToken);

        var retry = 50;
        while (retry > 0 && !response.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            retry--;
        }

        if (response is { IsCompleted: true, Result.Buffer.Length: > 0 })
        {
            var startIndex = 5;
            var bufferLenght = response.Result.Buffer.Length - 7;
            var value = new byte[bufferLenght];
            if (response.Result.Buffer.Length > startIndex)
            {
                Array.Copy(response.Result.Buffer, startIndex, value, 0, bufferLenght);
                return value;
            }

            logger.LogInformation("SerialNumber: Too short buffer");
            return Enumerable.Empty<byte>().ToArray();
        }
        else
        {
            logger.LogInformation("Timeout: IsCompleted=>{completed}", response.IsCompleted);
            throw new TimeoutException();
        }
    }

    public void Init(IPAddress address)
    {
        IpAddress = address;
    }

    private static UdpClient GetClient(IPAddress address)
    {
        var client = new UdpClient();
        client.Connect(address, 8899);
        return client;
    }
}