using System.Diagnostics;
using System.IO.Ports;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader.GoodWeCommunication.Connections;

public class RS485Connection : IConnection
{
    private readonly ILogger<RS485Connection> logger;

    private SerialPort? SerialPort { get; set; }

    public RS485Connection(ILogger<RS485Connection> logger)
    {
        this.logger = logger;
    }

    public async Task<byte[]> Send(byte[] message, CancellationToken cancellationToken)
    {
        var client = GetClient();

        client.Write(message, 0, message.Length);

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        var response = new byte[client.BytesToRead];
        client.Read(response, 0, response.Length);

        if (response.Length > 0)
        {
            var startIndex = 3;
            var bufferLenght = response.Length - 5;
            var value = new byte[bufferLenght];
            if (response.Length > startIndex)
            {
                Array.Copy(response, startIndex, value, 0, bufferLenght);
                return value;
            }

            logger.LogInformation("SerialNumber: Too short buffer");
            return Enumerable.Empty<byte>().ToArray();
        }
        else
        {
            logger.LogInformation("Timeout: IsCompleted=>{completed}", response.Length > 0);
            throw new TimeoutException();
        }
    }

    private SerialPort GetClient()
    {
        if (SerialPort?.IsOpen != true)
        {
            SerialPort?.Dispose();
            var portName = "/dev/ttyUSB0";
            if (Debugger.IsAttached)
                portName = "COM3";
            logger.LogInformation("Try to connect via COM {com}", portName);
            SerialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            SerialPort.Open();

            logger.LogInformation("Connected to COM {com}", portName);
        }
        return SerialPort;
    }
}