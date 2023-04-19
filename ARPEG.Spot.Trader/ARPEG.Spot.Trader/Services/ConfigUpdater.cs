using System.Text.Json;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Config.BitOutputs;
using ARPEG.Spot.Trader.Constants;
using Microsoft.Extensions.Options;

namespace ARPEG.Spot.Trader.Services;

public class ConfigUpdater : IConfigUpdater
{
    private readonly Root _optionsRoot;

    public ConfigUpdater(Root optionsRoot)
    {
        _optionsRoot = optionsRoot;
    }

    public Root GetCurrent()
        => _optionsRoot;

    public Task SaveCurrent(Root root,
        CancellationToken cancellationToken)
    {
        System.IO.FileInfo file = new System.IO.FileInfo(AppSettings.UserAppSettingsFile);
        file.Directory?.Create();
        return File.WriteAllTextAsync(AppSettings.UserAppSettingsFile, JsonSerializer.Serialize(root), cancellationToken);
    }
}