using ShieldCommand.Core.Models;
using Zeroconf;

namespace ShieldCommand.Core.Services;

public class DeviceDiscoveryService
{
    // Shield advertises as Android TV remote service
    private static readonly string[] ServiceTypes =
    [
        "_androidtvremote2._tcp.local.",
        "_androidtvremote._tcp.local.",
    ];

    public async Task<List<DiscoveredDevice>> ScanAsync(TimeSpan? scanTime = null, CancellationToken ct = default)
    {
        var timeout = scanTime ?? TimeSpan.FromSeconds(5);
        var devices = new List<DiscoveredDevice>();

        var results = await ZeroconfResolver.ResolveAsync(
            ServiceTypes,
            scanTime: timeout,
            cancellationToken: ct);

        foreach (var host in results)
        {
            var ip = host.IPAddress;
            var name = host.DisplayName;

            // Try to extract a friendlier name from TXT records
            foreach (var svc in host.Services.Values)
            {
                foreach (var prop in svc.Properties)
                {
                    if (prop.TryGetValue("fn", out var friendlyName) && !string.IsNullOrEmpty(friendlyName))
                    {
                        name = friendlyName;
                    }
                    else if (prop.TryGetValue("n", out var n) && !string.IsNullOrEmpty(n))
                    {
                        name = n;
                    }
                }
            }

            if (!devices.Any(d => d.IpAddress == ip))
            {
                devices.Add(new DiscoveredDevice(ip, name));
            }
        }

        return devices;
    }
}
