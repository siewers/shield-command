using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class LoadAvgCommand : IAdbShellCommand<int>
{
    public string Name => nameof(DynamicSections.ThreadCount);

    public string CommandText => "cat /proc/loadavg";

    public int Parse(ReadOnlySpan<char> output)
    {
        var trimmed = output.Trim();
        Span<Range> fields = stackalloc Range[6];
        var fieldCount = trimmed.Split(fields, ' ');
        if (fieldCount < 4)
        {
            return 0;
        }

        var runnable = trimmed[fields[3]];
        var slashIdx = runnable.IndexOf('/');
        if (slashIdx >= 0 && int.TryParse(runnable[(slashIdx + 1)..], out var threads))
        {
            return threads;
        }

        return 0;
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.ThreadCount = Parse(output);
}
