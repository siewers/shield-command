using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services;

public interface IDeviceDiscoveryService
{
    Task<List<DiscoveredDevice>> ScanAsync(TimeSpan? scanTime = null, CancellationToken ct = default);
}
