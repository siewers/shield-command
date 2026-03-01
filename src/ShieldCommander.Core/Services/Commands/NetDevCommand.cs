using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class NetDevCommand : IAdbShellCommand<NetworkSnapshot>
{
    public string Name => nameof(DynamicSections.Network);

    public string CommandText => "cat /proc/net/dev";

    public NetworkSnapshot Parse(ReadOnlySpan<char> output)
    {
        long bytesIn = 0, bytesOut = 0, packetsIn = 0, packetsOut = 0;
        Span<Range> vals = stackalloc Range[12];

        foreach (var line in output.EnumerateLines())
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
            {
                continue;
            }

            var iface = line[..colonIdx].Trim();
            if (iface.SequenceEqual("lo") || iface.StartsWith("Inter") || iface.StartsWith("face"))
            {
                continue;
            }

            var afterColon = line[(colonIdx + 1)..].Trim();
            var valCount = afterColon.Split(vals, ' ', StringSplitOptions.RemoveEmptyEntries);
            if (valCount < 10)
            {
                continue;
            }

            if (long.TryParse(afterColon[vals[0]], out var rxBytes))
            {
                bytesIn += rxBytes;
            }

            if (long.TryParse(afterColon[vals[1]], out var rxPackets))
            {
                packetsIn += rxPackets;
            }

            if (long.TryParse(afterColon[vals[8]], out var txBytes))
            {
                bytesOut += txBytes;
            }

            if (long.TryParse(afterColon[vals[9]], out var txPackets))
            {
                packetsOut += txPackets;
            }
        }

        return new NetworkSnapshot(bytesIn, bytesOut, packetsIn, packetsOut);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Network = Parse(output);
}
