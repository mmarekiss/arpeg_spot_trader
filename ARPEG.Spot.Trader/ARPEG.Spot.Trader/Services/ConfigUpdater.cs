using System.Text.Json;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Constants;
using Microsoft.Extensions.Options;

namespace ARPEG.Spot.Trader.Services;

public class ConfigUpdater
{
    private readonly IOptionsMonitor<Root> _optionsRoot;

    public ConfigUpdater(IOptionsMonitor<Root> optionsRoot)
    {
        _optionsRoot = optionsRoot;
    }

    public Root GetCurrent()
        => _optionsRoot.CurrentValue;

    public Task SaveCurrent(Root root, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(AppSettings.UserAppSettingsFile, JsonSerializer.Serialize(root), cancellationToken);
}