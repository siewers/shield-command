using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class ProcessCountCommand : IAdbShellCommand<int>
{
    public string Name => nameof(DynamicSections.ProcessCount);

    public string CommandText => "ls /proc/";

    public int Parse(ReadOnlySpan<char> output)
    {
        var count = 0;
        foreach (var line in output.EnumerateLines())
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && ParseHelper.IsAllDigits(trimmed))
            {
                count++;
            }
        }

        return count;
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.ProcessCount = Parse(output);
}
