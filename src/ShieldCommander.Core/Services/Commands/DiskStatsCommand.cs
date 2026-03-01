using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class DiskStatsCommand : IAdbShellCommand<DiskSnapshot>
{
    public string Name => nameof(DynamicSections.Disk);

    public string CommandText =>
        "grep -E '^pgpgin |^pgpgout ' /proc/vmstat; dumpsys diskstats | grep -E 'Latency:|Recent Disk Write Speed'";

    public DiskSnapshot Parse(string output)
    {
        long bytesRead = 0;
        long bytesWritten = 0;
        var writeLatencyMs = 0;
        long writeSpeed = 0;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            var parts = trimmed.Split(' ');

            if (parts[0] == "pgpgin" && parts.Length >= 2 && long.TryParse(parts[1], out var pgIn))
            {
                bytesRead = pgIn * 1024;
            }
            else if (parts[0] == "pgpgout" && parts.Length >= 2 && long.TryParse(parts[1], out var pgOut))
            {
                bytesWritten = pgOut * 1024;
            }
            else if (trimmed.StartsWith("Latency:"))
            {
                var msIdx = trimmed.IndexOf("ms", StringComparison.Ordinal);
                if (msIdx > 0)
                {
                    var numStr = trimmed["Latency:".Length..msIdx].Trim();
                    if (int.TryParse(numStr, out var ms))
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

                var numStr = trimmed[(eqIdx + 1)..].Trim();
                if (long.TryParse(numStr, out var speed))
                {
                    writeSpeed = speed * 1024;// KB/s -> bytes/s
                }
            }
        }

        return new DiskSnapshot(bytesRead, bytesWritten, writeLatencyMs, writeSpeed);
    }

    public void Apply(string output, DynamicSections target)
        => target.Disk = Parse(output);
}
