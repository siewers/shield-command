using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class NetDevCommand : IAdbShellCommand<NetworkSnapshot>
{
    public string Name => nameof(DynamicSections.Network);

    public string CommandText => "cat /proc/net/dev";

    public NetworkSnapshot Parse(string output)
    {
        long bytesIn = 0, bytesOut = 0, packetsIn = 0, packetsOut = 0;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
            {
                continue;
            }

            var iface = line[..colonIdx].Trim();
            if (iface == "lo" || iface.StartsWith("Inter") || iface.StartsWith("face"))
            {
                continue;
            }

            var vals = line[(colonIdx + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length < 10)
            {
                continue;
            }

            if (long.TryParse(vals[0], out var rxBytes))
            {
                bytesIn += rxBytes;
            }

            if (long.TryParse(vals[1], out var rxPackets))
            {
                packetsIn += rxPackets;
            }

            if (long.TryParse(vals[8], out var txBytes))
            {
                bytesOut += txBytes;
            }

            if (long.TryParse(vals[9], out var txPackets))
            {
                packetsOut += txPackets;
            }
        }

        return new NetworkSnapshot(bytesIn, bytesOut, packetsIn, packetsOut);
    }

    public void Apply(string output, DynamicSections target)
        => target.Network = Parse(output);
}
