﻿using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using SshNet.Security.Cryptography;

namespace ARPEG.Spot.Trader.GoodWeCommunication.Connections;

public class RS485Connection : IConnection
{
    private readonly ILogger<RS485Connection> logger;

    public RS485Connection(ILogger<RS485Connection> logger)
    {
        this.logger = logger;
    }


    public async Task<byte[]> Send(byte[] message, CancellationToken cancellationToken)
    {  
        var client = GetClient();
        try
        {
            client.Open();
            client.Write(message, 0, message.Length);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            var response = new byte[client.BytesToRead];
            client.Read(response, 0, response.Length);


            if (response.Length > 0 )
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
        finally
        {
            client?.Dispose();
        }
    }

    private static SerialPort GetClient()
    {
        return new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
    }
}