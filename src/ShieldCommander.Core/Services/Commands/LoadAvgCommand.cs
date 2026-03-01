using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class LoadAvgCommand : IAdbShellCommand<int>
{
    public string Name => nameof(DynamicSections.ThreadCount);

    public string CommandText => "cat /proc/loadavg";

    public int Parse(string output)
    {
        var fields = output.Trim().Split(' ');
        if (fields.Length < 4)
        {
            return 0;
        }

        var parts = fields[3].Split('/');
        if (parts.Length == 2 && int.TryParse(parts[1], out var threads))
        {
            return threads;
        }

        return 0;
    }

    public void Apply(string output, DynamicSections target)
        => target.ThreadCount = Parse(output);
}
