﻿using System.Device.Gpio;
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

    private TOptions Options { get; set; }

    public BitController(ILogger<BitController<TOptions>> logger,
        IOptionsMonitor<TOptions> optionsMonitor,
        IEnumerable<IDataValueHandler> handlers)
    {
        _logger = logger;
        Options = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(Listener); 
        _handlers = handlers;
        _gauge = Metrics.CreateGauge("Outputs", "Digital outputs", "output");
    }

    private void Listener(TOptions opt, string arg2)
    {
        Options = opt;
    }

    public Task HandleDataValue(Definition inverterDefinition,
        DataValue dataValue)
    {
        if (inverterDefinition.SN != Options.GwSn) return Task.CompletedTask;
        
        var handler = _handlers.FirstOrDefault(x => x.Type == Options.DriverType);

        var value = handler?.Handle(dataValue, Options.GreaterThen, Options.TriggerValue);
        if (!value.HasValue) return Task.CompletedTask;
        
        _gauge.WithLabels(Options.Pin.ToString()).Set(value.Value ? 1 : 0);
        _logger.LogTrace("Set output for pin {pinId} {value}", Options.Pin, value);

#if !DEBUG
        using var controller = new GpioController();
        controller.OpenPin(Options.Pin, PinMode.Output);
        controller.Write(Options.Pin, value.Value);
#endif  
        
        return Task.CompletedTask;
    }
}