using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class MemInfoQuery : IAdbShellQuery<MemoryInfo>
{
    public string Name => nameof(DynamicSections.Memory);

    public string CommandText => "cat /proc/meminfo";

    public MemoryInfo Parse(ReadOnlySpan<char> output)
    {
        long total = 0;
        long available = 0;
        long free = 0;
        long buffers = 0;
        long cached = 0;
        long swapTotal = 0;
        long swapFree = 0;

        foreach (var line in output.EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("MemTotal:"))
            {
                total = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("MemFree:"))
            {
                free = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("MemAvailable:"))
            {
                available = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("Buffers:"))
            {
                buffers = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("Cached:"))
            {
                cached = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("SwapTotal:"))
            {
                swapTotal = trimmed.KbToBytes();
            }
            else if (trimmed.StartsWith("SwapFree:"))
            {
                swapFree = trimmed.KbToBytes();
            }
        }

        var snapshot = new MemorySnapshot(total, available, free, buffers, cached, swapTotal, swapFree);
        return new MemoryInfo(snapshot, total);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Memory = Parse(output);
}
