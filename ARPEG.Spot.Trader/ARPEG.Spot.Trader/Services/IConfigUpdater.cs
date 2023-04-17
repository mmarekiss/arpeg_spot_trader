using ARPEG.Spot.Trader.Config;

namespace ARPEG.Spot.Trader.Services;

public interface IConfigUpdater
{
    Root GetCurrent();
    Task SaveCurrent(Root root, CancellationToken cancellationToken);
}