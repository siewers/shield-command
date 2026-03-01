using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class UptimeCommand : IAdbShellCommand<string?>
{
    public string Name => nameof(DynamicSections.Uptime);

    public string CommandText => "uptime";

    public string? Parse(string output)
    {
        var uptimeOutput = output.Trim();
        var upIdx = uptimeOutput.IndexOf("up ", StringComparison.Ordinal);
        if (upIdx < 0)
        {
            return null;
        }

        var rest = uptimeOutput[(upIdx + 3)..];
        var commaIdx = rest.IndexOf(',');
        if (commaIdx > 0)
        {
            var afterFirst = rest[(commaIdx + 1)..];
            var secondComma = afterFirst.IndexOf(',');
            if (secondComma > 0 && afterFirst[..secondComma].Trim().Contains(':'))
            {
                return rest[..commaIdx].Trim();
            }

            if (secondComma > 0)
            {
                return rest[..(commaIdx + 1 + secondComma)].Trim().TrimEnd(',');
            }

            return rest[..commaIdx].Trim();
        }

        return rest.Trim();
    }

    public void Apply(string output, DynamicSections target)
        => target.Uptime = Parse(output);
}
