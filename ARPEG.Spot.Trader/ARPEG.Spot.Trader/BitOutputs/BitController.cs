using System.Device.Gpio;
using ARPEG.Spot.Trader.BitOutputs.Handlers;
using ARPEG.Spot.Trader.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.BitOutputs;

public class BitController<TOptions> : IBitController, IDisposable
    where TOptions : BitOutputOptions
{
    // private readonly Gauge _gauge;
    private readonly IEnumerable<IDataValueHandler> _handlers;
    private readonly ILogger<BitController<TOptions>> _logger;
    private readonly TOptions _options;

#if !DEBUG
    private readonly GpioController _controller;
#endif


    public BitController(ILogger<BitController<TOptions>> logger,
        TOptions options,
        IEnumerable<IDataValueHandler> handlers)
    {
        _logger = logger;
        _options = options;
        _handlers = handlers;
        // _gauge = Metrics.CreateGauge("Outputs", "Digital outputs", "output", "description", "output_"+Guid.NewGuid().ToString());
#if !DEBUG
        _controller = new GpioController();
        _controller.OpenPin( _options.Pin, PinMode.Output);
#endif
    }

    public Task HandleDataValue(Definition inverterDefinition,
        DataValue dataValue)
    {
        if (inverterDefinition.SN != _options.GwSn) return Task.CompletedTask;
        
        var handler = _handlers.FirstOrDefault(x => x.Type == _options.DriverType);

        var value = handler?.Handle(dataValue, _options );

        var description = CreateDescription(_options);
        // foreach (var lbl in _gauge.GetAllLabelValues().Where(x=> x[0] == _options.Pin.ToString() && x[1] != description)) //Remove old descriptions
        // {
        //     _gauge.RemoveLabelled(lbl);
        // }
        //
        // if (!value.HasValue)
        // {
        //     if (!_gauge.GetAllLabelValues().Any(x => x[0] == _options.Pin.ToString()))
        //     {
        //         _gauge.WithLabels(_options.Pin.ToString(), description).Set(0);
        //     }
        //
        //     return Task.CompletedTask;
        // }
        //
        // _gauge.WithLabels(_options.Pin.ToString(), description).Set(value.Value ? 1 : 0);
        _logger.LogTrace("Set output for pin {pinId} {value}", _options.Pin, value);
        
#if !DEBUG
        _controller.Write(_options.Pin, !value.Value);
#endif  
        
        return Task.CompletedTask;
    }

    private string CreateDescription(TOptions options)
    {
        var mark = _options.GreaterThen ? ">" : "<";
        return $"{_options.DriverType} {mark} { _options.TriggerValue}";
    }

    public void Dispose()
    {
        
#if !DEBUG
        _controller.Dispose();
#endif
        
    }
}