﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace TecoBridge.GoodWe;

public class GoodWeCom
{
    private readonly Dictionary<string, Gauge> _gauges = new();
    private readonly ILogger<GoodWeCom> _logger;

    public GoodWeCom(ILogger<GoodWeCom> logger)
    {
        _logger = logger;
    }

    private string Name { get; set; }

    private IPAddress? IpAddress { get; set; }

    public void InitHostname(IPAddress ipAddress)
    {
        IpAddress = ipAddress;
    }

    public void InitInverterName(string name)
    {
        Name = name;
    }


    private UdpClient GetClient()
    {
        if (IpAddress is null)
            throw new ArgumentNullException("IpAddress is not initialized");
        var client = new UdpClient();
        client.Connect(IpAddress, 8899);
        return client;
    }


    public async Task<string?> GetInverterName()
    {
        _logger.LogInformation("Try check address {0}", IpAddress);
        ushort minAddress = 35003;
        var reqRegisters = 8;
        var arr = new byte[]
            { 0xF7, 0x03, (byte)(minAddress >> 8), (byte)minAddress, (byte)(reqRegisters >> 8), (byte)reqRegisters };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        using var client = GetClient();
        await client.SendAsync(arr);

        var response = client.ReceiveAsync();

        await Task.Delay(TimeSpan.FromMilliseconds(300));

        if (response is { IsCompleted: true, Result.Buffer.Length: > 0 })
        {
            var startIndex = 5;
            var value = new byte[16];
            if (response.Result.Buffer.Length > startIndex)
            {
                Array.Copy(response.Result.Buffer, startIndex, value, 0, 16);
                return Encoding.ASCII.GetString(value);
            }

            _logger.LogInformation("SerialNumber: Too short buffer");
        }
        else
        {
            _logger.LogInformation("Timeout");
        }

        return null;
    }

    public async Task SetExportLimit(UInt16 limit, CancellationToken cancellationToken)
    {
        await SetUint16Value(47510, limit, cancellationToken);
    }
    
    public async Task ForceBatteryCharge(CancellationToken cancellationToken)
    {
        await SetUint16Value(47517, 100, cancellationToken);
    }
    
    public async Task StopForceBatteryCharge(CancellationToken cancellationToken)
    {
        await SetUint16Value(47517, 0, cancellationToken);
    }

    public async Task GetHomeConsumption()
    {
        _logger.LogInformation("=============================");
        await GetInt16Values(
            new DataPoint("Grid", "L1", 36020),
            new DataPoint("Grid", "L2", 36022),
            new DataPoint("Grid", "L3", 36024),
            new DataPoint("Grid", "Total", 36026)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Export", "Limit", 47510),
            new DataPoint("Battery", "GridCharge FROM ", 47515),
            new DataPoint("Battery", "GridCharge TO", 47516),
            new DataPoint("Battery", "GridCharge", 47517)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Battery", "V", 35180),
            new DataPoint("Battery", "I", 35181),
            new DataPoint("Battery", "W", 35183),
            new DataPoint("Battery", "Mode", 35184)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Battery", "BMS", 37002),
            new DataPoint("Battery", "Temperature", 37003),
            new DataPoint("Battery", "ChargeImax", 37004),
            new DataPoint("Battery", "DischargeImax", 37005),
            new DataPoint("Battery", "bmErrCode", 37006),
            new DataPoint("Battery", "SOC", 37007),
            new DataPoint("Battery", "cmsSOH", 37008)
        ).ToListAsync();


        await GetInt16Values(
            new DataPoint("Backup V", "L1", 35145),
            new DataPoint("Backup I", "L1", 35146),
            new DataPoint("Backup Freq", "L1", 35147),
            new DataPoint("Backup Mode", "L1", 35148),
            new DataPoint("Backup Watt", "L1", 35150),
            new DataPoint("Backup V", "L2", 35151),
            new DataPoint("Backup I", "L2", 35152),
            new DataPoint("Backup Freq", "L2", 35153),
            new DataPoint("Backup Mode", "L2", 35154),
            new DataPoint("Backup Watt", "L2", 35156),
            new DataPoint("Backup V", "L3", 35157),
            new DataPoint("Backup I", "L3", 35158),
            new DataPoint("Backup Freq", "L3", 35159),
            new DataPoint("Backup Mode", "L3", 35160),
            new DataPoint("Backup Watt", "L3", 35162),
            new DataPoint("Backup Watt", "Total", 35170)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Feed", "L1", 35125),
            new DataPoint("Feed", "L2", 35130),
            new DataPoint("Feed", "L3", 35135),
            new DataPoint("Feed", "Total", 35138)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Consumption", "L1", 35164),
            new DataPoint("Consumption", "L2", 35166),
            new DataPoint("Consumption", "L3", 35168),
            new DataPoint("Consumption", "Total", 35172)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("PV", "PV1_Voltage", 35103),
            new DataPoint("PV", "PV1_Current", 35104),
            new DataPoint("PV", "PV1_Wats", 35106),
            new DataPoint("PV", "PV2_Voltage", 35107),
            new DataPoint("PV", "PV2_Current", 35108),
            new DataPoint("PV", "PV2_Wats", 35110)
        ).ToListAsync();

        await GetInt16Values(
            new DataPoint("Temperature", "Temperature_Air", 35174),
            new DataPoint("Temperature", "Temperature_Radiator", 35176),
            new DataPoint("Temperature", "Temperature_Module", 35175)
        ).ToListAsync();
    }

    private void TraceGauge(string group,
        string part,
        short value)
    {
        var id = $"{group}";
        if (!_gauges.TryGetValue(id, out var gauge))
            _gauges.Add(group, gauge = Metrics.CreateGauge(group.Replace(" ", "_"), "GoodWe traced value",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "part", "sn" }
                }));

        gauge?.WithLabels(part, Name).Set(value);
    }

    private async Task SetUint16Value(UInt16 address,
        UInt16 value, CancellationToken cancellationToken)
    {
        var arr = new byte[]
            { 0xF7, 0x06, (byte)(address >> 8), (byte)address, (byte)(value >> 8), (byte)value };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        using var client = GetClient();
        await client.SendAsync(arr, cancellationToken);
    }

    private async IAsyncEnumerable<DataValue> GetInt16Values(params DataPoint[] points)
    {
        await Task.Delay(500);
        var minAddress = points.Min(x => x.Address);
        var maxAddress = points.Max(x => x.Address);
        var reqRegisters = maxAddress - minAddress + 1;

        var arr = new byte[]
            { 0xF7, 0x03, (byte)(minAddress >> 8), (byte)minAddress, (byte)(reqRegisters >> 8), (byte)reqRegisters };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        using var client = GetClient();
        await client.SendAsync(arr);

        var response = client.ReceiveAsync();
        var retry = 50;
        while (retry > 0 && !response.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            retry--;
        }

        if (response is { IsCompleted: true, Result.Buffer.Length: > 0 })
            foreach (var point in points)
            {
                var value = new byte[2];
                var startIndex = 5 + (point.Address - minAddress) * 2;
                if (response.Result.Buffer.Length > startIndex)
                {
                    Array.Copy(response.Result.Buffer, startIndex, value, 0, 2);
                    var result = BitConverter.ToInt16(value.Reverse().ToArray());
                    _logger.LogInformation($"{point.Description}: {result}");
                    TraceGauge(point.Group, point.Description, result);
                    yield return new DataValue(point.Address, point.Description, result);
                }
                else
                {
                    _logger.LogInformation($"{point.Description}: Too short buffer");
                }
            }
        else
            _logger.LogInformation("Timeout");
    }
}