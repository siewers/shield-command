using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class DiskStatsCommand : IAdbShellCommand<DiskSnapshot>
{
    public string Name => nameof(DynamicSections.Disk);

    public string CommandText =>
        "grep -E '^pgpgin |^pgpgout ' /proc/vmstat; dumpsys diskstats | grep -E 'Latency:|Recent Disk Write Speed'";

    public DiskSnapshot Parse(ReadOnlySpan<char> output)
    {
        long bytesRead = 0;
        long bytesWritten = 0;
        var writeLatencyMs = 0;
        long writeSpeed = 0;
        Span<Range> parts = stackalloc Range[4];

        foreach (var line in output.EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.IsEmpty)
            {
                continue;
            }

            var partCount = trimmed.Split(parts, ' ');

            if (trimmed[parts[0]] is "pgpgin" &&
                partCount >= 2 &&
                long.TryParse(trimmed[parts[1]], out var pgIn))
            {
                bytesRead = pgIn * 1024;
            }
            else if (trimmed[parts[0]] is "pgpgout" &&
                     partCount >= 2 &&
                     long.TryParse(trimmed[parts[1]], out var pgOut))
            {
                bytesWritten = pgOut * 1024;
            }
            else if (trimmed.StartsWith("Latency:"))
            {
                var msIdx = trimmed.IndexOf("ms");
                if (msIdx > 0)
                {
                    var numSpan = trimmed["Latency:".Length..msIdx].Trim();
                    if (int.TryParse(numSpan, out var ms))
                    {
                        writeLatencyMs = ms;
                    }
                }
            }
            else if (trimmed.StartsWith("Recent Disk Write Speed"))
            {
                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0)
                {
                    continue;
                }

                var numSpan = trimmed[(eqIdx + 1)..].Trim();
                if (long.TryParse(numSpan, out var speed))
                {
                    writeSpeed = speed * 1024;// KB/s -> bytes/s
                }
            }
        }

        return new DiskSnapshot(bytesRead, bytesWritten, writeLatencyMs, writeSpeed);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Disk = Parse(output);
}
