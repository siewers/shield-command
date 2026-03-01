using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class ProcessCountCommand : IAdbShellCommand<int>
{
    public string Name => nameof(DynamicSections.ProcessCount);

    public string CommandText => "ls /proc/";

    public int Parse(string output)
    {
        var count = 0;
        foreach (var entry in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = entry.AsSpan().Trim();
            if (trimmed.Length > 0 && ParseHelper.IsAllDigits(trimmed))
            {
                count++;
            }
        }

        return count;
    }

    public void Apply(string output, DynamicSections target)
        => target.ProcessCount = Parse(output);
}
