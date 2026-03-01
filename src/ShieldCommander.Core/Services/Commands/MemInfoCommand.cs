using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class MemInfoCommand : IAdbShellCommand<MemoryInfo>
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
                total = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("MemFree:"))
            {
                free = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("MemAvailable:"))
            {
                available = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("Buffers:"))
            {
                buffers = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("Cached:"))
            {
                cached = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("SwapTotal:"))
            {
                swapTotal = ParseHelper.KbToBytes(trimmed);
            }
            else if (trimmed.StartsWith("SwapFree:"))
            {
                swapFree = ParseHelper.KbToBytes(trimmed);
            }
        }

        var snapshot = new MemorySnapshot(total, available, free, buffers, cached, swapTotal, swapFree);
        return new MemoryInfo(snapshot, total);
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Memory = Parse(output);
}
