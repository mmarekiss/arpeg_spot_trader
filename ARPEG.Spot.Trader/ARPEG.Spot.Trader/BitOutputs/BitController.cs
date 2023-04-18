using ARPEG.Spot.Trader.BitOutputs.Handlers;
using ARPEG.Spot.Trader.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs;

public class BitController<TOptions> : IBitController
    where TOptions : BitOutputOptions
{
    private readonly Gauge _gauge;
    private readonly IEnumerable<IDataValueHandler> _handlers;
    private readonly ILogger<BitController<TOptions>> _logger;
    private readonly IOptionsMonitor<TOptions> _optionsMonitor;

    public BitController(ILogger<BitController<TOptions>> logger,
        IOptionsMonitor<TOptions> optionsMonitor,
        IEnumerable<IDataValueHandler> handlers)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _handlers = handlers;
        _gauge = Metrics.CreateGauge("Outputs", "Digital outputs", "output");
    }

    public Task HandleDataValue(Definition inverterDefinition,
        DataValue dataValue)
    {
        var opt = _optionsMonitor.CurrentValue;
        if (inverterDefinition.SN != opt.GwSn) return Task.CompletedTask;
        
        var handler = _handlers.FirstOrDefault(x => x.Type == opt.DriverType);

        var value = handler?.Handle(dataValue, opt.GreaterThen, opt.TriggerValue);
        if (!value.HasValue) return Task.CompletedTask;
        
        _gauge.WithLabels(opt.Pin.ToString()).Set(value.Value ? 1 : 0);
        _logger.LogTrace("Set output for pin {pinId} {value}", opt.Pin, value);

        return Task.CompletedTask;
    }
}