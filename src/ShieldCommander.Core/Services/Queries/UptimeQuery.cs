using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Queries;

internal sealed class UptimeQuery : IAdbShellQuery<UptimeInfo?>
{
    public string Name => nameof(DynamicSections.Uptime);

    public string CommandText => "uptime";

    public UptimeInfo? Parse(ReadOnlySpan<char> output)
    {
        var uptimeOutput = output.Trim();
        var upIdx = uptimeOutput.IndexOf("up ");
        if (upIdx < 0)
        {
            return null;
        }

        var rest = uptimeOutput[(upIdx + 3)..];
        var commaIdx = rest.IndexOf(',');

        ReadOnlySpan<char> uptimeSpan;
        if (commaIdx > 0)
        {
            var afterFirst = rest[(commaIdx + 1)..];
            var secondComma = afterFirst.IndexOf(',');
            if (secondComma > 0 && afterFirst[..secondComma].Trim().Contains(':'))
            {
                uptimeSpan = rest[..commaIdx].Trim();
            }
            else if (secondComma > 0)
            {
                uptimeSpan = rest[..(commaIdx + 1 + secondComma)].Trim().TrimEnd(',');
            }
            else
            {
                uptimeSpan = rest[..commaIdx].Trim();
            }
        }
        else
        {
            uptimeSpan = rest.Trim();
        }

        return new UptimeInfo(ParseDuration(uptimeSpan));
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.Uptime = Parse(output);

    private static TimeSpan ParseDuration(ReadOnlySpan<char> span)
    {
        var days = 0;
        var hours = 0;
        var minutes = 0;

        // Check for "X days" or "X day" prefix
        var dayIdx = span.IndexOf("day");
        if (dayIdx > 0)
        {
            int.TryParse(span[..dayIdx].Trim(), out days);
            // Skip past "days" or "day" and any comma
            var afterDay = span[(dayIdx + 3)..];
            if (afterDay.Length > 0 && afterDay[0] == 's')
            {
                afterDay = afterDay[1..];
            }

            afterDay = afterDay.TrimStart(',').Trim();
            span = afterDay;
        }

        // Parse "H:MM" or just minutes
        var colonIdx = span.IndexOf(':');
        if (colonIdx >= 0)
        {
            int.TryParse(span[..colonIdx].Trim(), out hours);
            int.TryParse(span[(colonIdx + 1)..].Trim(), out minutes);
        }
        else if (span.IndexOf("min") >= 0)
        {
            var minIdx = span.IndexOf("min");
            int.TryParse(span[..minIdx].Trim(), out minutes);
        }

        return new TimeSpan(days, hours, minutes, 0);
    }
}
