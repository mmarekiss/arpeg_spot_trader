using System.Text;
using ARPEG.Spot.Trader.BitOutputs;
using ARPEG.Spot.Trader.Constants;
using ARPEG.Spot.Trader.GoodWeCommunication;
using ARPEG.Spot.Trader.GoodWeCommunication.Connections;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace TecoBridge.GoodWe;

public class GoodWeCom
{
    private readonly IEnumerable<IBitController> bitControllers;
    private readonly Dictionary<string, Gauge> gauges = new();
    private readonly ILogger<GoodWeCom> logger;

    public GoodWeCom(ILogger<GoodWeCom> logger,
        IEnumerable<IBitController> bitControllers)
    {
        this.logger = logger;
        this.bitControllers = bitControllers;
    }

    public async Task<(string? sn, IConnection? connection)> GetInverterName(IConnection connection)
    {
        ushort minAddress = 35003;
        var reqRegisters = 8;
        var arr = new byte[]
            { 0xF7, 0x03, (byte)(minAddress >> 8), (byte)minAddress, (byte)(reqRegisters >> 8), (byte)reqRegisters };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        try
        {
            var value = await connection.Send(arr, default);
            return (Encoding.ASCII.GetString(value), connection);
        }
        catch (TimeoutException exception)
        {
            logger.LogInformation(exception, "This is not GW");
        }

        return (null, null);
    }

    public async Task SetExportLimit(ushort limit,
        Definition definition,
        CancellationToken cancellationToken)
    {
        await SetUint16Value(47509, 1, definition, cancellationToken);
        await SetUint16Value(47510, limit, definition, cancellationToken);
    }

    public async Task DisableExportLimit(Definition definition,
        CancellationToken cancellationToken)
    {
        await SetUint16Value(47509, 0, definition, cancellationToken);
        await SetUint16Value(47510, 10000, definition, cancellationToken);
    }

    public async Task ForceBatteryCharge(Definition definition,
        ushort chargePower,
        CancellationToken cancellationToken)
    {
        await SetUint16Value(47512, chargePower, definition, cancellationToken);
        await SetUint16Value(47511, 2, definition, cancellationToken);
    }

    public async Task ForceBatteryDisCharge(Definition definition,
        ushort chargePower,
        CancellationToken cancellationToken)
    {
        await SetUint16Value(47512, chargePower, definition, cancellationToken);
        await SetUint16Value(47511, 5, definition, cancellationToken);
    }

    public async Task SetSelfConsumptionMode(Definition definition,
        CancellationToken cancellationToken)
    {
        await SetUint16Value(47511, 1, definition, cancellationToken);
    }

    public async Task GetHomeConsumption(
        Definition definition,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("=============================");
        await GetInt16Values(
            definition,
            new DataPoint("Grid", "L1", 36020, Max: 10_000),
            new DataPoint("Grid", "L2", 36022, Max: 10_000),
            new DataPoint("Grid", "L3", 36024, Max: 10_000),
            new DataPoint("Grid", "Total", 36026, Max: 10_000)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint("Export", "Enabled", 47509, 0, 1),
            new DataPoint("Export", "Limit", 47510, 0),
            new DataPoint("Trade", "Mode", 47511, 0),
            new DataPoint("Trade", "Charge", 47512, 0),
            new DataPoint(DataGroupNames.Battery, "GridCharge FROM ", 47515),
            new DataPoint(DataGroupNames.Battery, "GridCharge TO", 47516),
            new DataPoint(DataGroupNames.Battery, "GridCharge", 47517)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint(DataGroupNames.Battery, "V Max", 45352),
            new DataPoint(DataGroupNames.Battery, "I Max", 45353),
            new DataPoint(DataGroupNames.Battery, "Volt Under Min", 45354),
            new DataPoint(DataGroupNames.Battery, "DisCurrMax", 45355, 0),
            new DataPoint(DataGroupNames.Battery, "SOC Min ", 45356, 0, 100),
            new DataPoint(DataGroupNames.Battery, "Offline Volt Min", 45357),
            new DataPoint(DataGroupNames.Battery, "Offline SOC Min", 45358, 0, 100)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint(DataGroupNames.Battery, "V", 35180, 0),
            new DataPoint(DataGroupNames.Battery, "I", 35181),
            new DataPoint(DataGroupNames.Battery, "W", 35183),
            new DataPoint(DataGroupNames.Battery, "Mode", 35184, 0, 4)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint(DataGroupNames.Battery, "BMS", 37002),
            new DataPoint(DataGroupNames.Battery, "Temperature", 37003),
            new DataPoint(DataGroupNames.Battery, "ChargeImax", 37004),
            new DataPoint(DataGroupNames.Battery, "DischargeImax", 37005),
            new DataPoint(DataGroupNames.Battery, "bmErrCode", 37006),
            new DataPoint(DataGroupNames.Battery, BatteryGroupParts.SOC, 37007, 0, 100),
            new DataPoint(DataGroupNames.Battery, "bmsSOH", 37008)
        ).ToListAsync(cancellationToken);


        await GetInt16Values(
            definition,
            new DataPoint("Backup V", "L1", 35145),
            new DataPoint("Backup I", "L1", 35146),
            new DataPoint("Backup Freq", "L1", 35147),
            new DataPoint("Backup Mode", "L1", 35148),
            new DataPoint("Backup Watt", "L1", 35150, Max: 10_000),
            new DataPoint("Backup V", "L2", 35151),
            new DataPoint("Backup I", "L2", 35152),
            new DataPoint("Backup Freq", "L2", 35153),
            new DataPoint("Backup Mode", "L2", 35154),
            new DataPoint("Backup Watt", "L2", 35156, Max: 10_000),
            new DataPoint("Backup V", "L3", 35157),
            new DataPoint("Backup I", "L3", 35158),
            new DataPoint("Backup Freq", "L3", 35159),
            new DataPoint("Backup Mode", "L3", 35160),
            new DataPoint("Backup Watt", "L3", 35162, Max: 10_000),
            new DataPoint("Backup Watt", "Total", 35170, Max: 10_000)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint("Feed", "L1", 35125, -10, 10_000),
            new DataPoint("Feed", "L2", 35130, -10, 10_000),
            new DataPoint("Feed", "L3", 35135, -10, 10_000),
            new DataPoint("Feed", "Total", 35138, -10, 10_000)
        ).ToListAsync(cancellationToken);

        await GetInt16Values(
            definition,
            new DataPoint("Consumption", "L1", 35164, 0, 10_000),
            new DataPoint("Consumption", "L2", 35166, 0, 10_000),
            new DataPoint("Consumption", "L3", 35168, 0, 10_000),
            new DataPoint("Consumption", "Total", 35172, 0, 10_000)
        ).ToListAsync(cancellationToken);

        var data = await GetInt16Values(
            definition,
            new DataPoint(DataGroupNames.PV, "PV1_Voltage", 35103, 0),
            new DataPoint(DataGroupNames.PV, "PV1_Current", 35104, 0),
            new DataPoint(DataGroupNames.PV, PVGroupParts.PV1_Wats, 35106, 0),
            new DataPoint(DataGroupNames.PV, "PV2_Voltage", 35107, 0),
            new DataPoint(DataGroupNames.PV, "PV2_Current", 35108, 0),
            new DataPoint(DataGroupNames.PV, PVGroupParts.PV2_Wats, 35110, 0)
        ).ToListAsync(cancellationToken);

        var sum = data.Where(x => x.part == PVGroupParts.PV2_Wats || x.part == PVGroupParts.PV1_Wats).Sum(x => x.value);
        foreach (var bitController in bitControllers)
            _ = bitController.HandleDataValue(definition,
                new DataValue(0, DataGroupNames.PV, PVGroupParts.Wats, (short)sum));
        await GetInt16Values(
            definition,
            new DataPoint(DataGroupNames.Temperature, TemperatureGroupParts.Temperature_Air, 35174, 0, 800),
            new DataPoint("Temperature", "Temperature_Radiator", 35176, 0, 800),
            new DataPoint("Temperature", "Temperature_Module", 35175, 0, 800)
        ).ToListAsync(cancellationToken);
    }

    private void TraceGauge(
        Definition definition,
        string group,
        string part,
        short value)
    {
        var id = $"{group}";
        if (!gauges.TryGetValue(id, out var gauge))
            gauges.Add(group, gauge = Metrics.CreateGauge(group.Replace(" ", "_"), "GoodWe traced value",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "part", "sn" }
                }));

        gauge?.WithLabels(part, definition.Sn).Set(value);
    }

    private async Task SetUint16Value(ushort address,
        ushort value,
        Definition definition,
        CancellationToken cancellationToken)
    {
        var arr = new byte[]
            { 0xF7, 0x06, (byte)(address >> 8), (byte)address, (byte)(value >> 8), (byte)value };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        await definition.Connection.Send(arr, cancellationToken);
    }

    private async IAsyncEnumerable<DataValue> GetInt16Values(
        Definition definition,
        params DataPoint[] points)
    {
        await Task.Delay(1000);
        var minAddress = points.Min(x => x.Address);
        var maxAddress = points.Max(x => x.Address);
        var reqRegisters = maxAddress - minAddress + 1;

        var arr = new byte[]
            { 0xF7, 0x03, (byte)(minAddress >> 8), (byte)minAddress, (byte)(reqRegisters >> 8), (byte)reqRegisters };

        var crc = ModbusCreator.CalculateCrc(arr, arr.Length);

        arr = arr.Concat(BitConverter.GetBytes(crc)).ToArray();

        byte[] response;
        try
        {
            response = await definition.Connection.Send(arr, default);
        }
        catch (TimeoutException exc)
        {
            logger.LogWarning("Timeout");
            yield break;
        }


        if (response.Length > 0)
            foreach (var point in points)
            {
                var value = new byte[2];
                var startIndex = (point.Address - minAddress) * 2;
                if (response.Length > startIndex + 1)
                {
                    Array.Copy(response, startIndex, value, 0, 2);
                    var result = BitConverter.ToInt16(value.Reverse().ToArray());
                    result = FitLimits(result, point);
                    logger.LogTrace($"{point.Description}: {result}");
                    TraceGauge(definition, point.Group, point.Description, result);
                    var resultValue = new DataValue(point.Address, point.Group, point.Description,
                        FitLimits(result, point));
                    foreach (var bitController in bitControllers)
                        _ = bitController.HandleDataValue(definition, resultValue);

                    yield return resultValue;
                }
                else
                {
                    logger.LogInformation($"{point.Description}: Too short buffer");
                }
            }
        else
            logger.LogInformation("Timeout");
    }

    private short FitLimits(short result,
        DataPoint point)
    {
        if (point.Min.HasValue && result < point.Min.Value)
            return (short)point.Min.Value;
        if (point.Max.HasValue && result > point.Max.Value)
            return (short)point.Max.Value;
        return result;
    }
}