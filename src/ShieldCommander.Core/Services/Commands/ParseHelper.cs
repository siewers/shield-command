using System.Buffers;
using System.Globalization;

namespace ShieldCommander.Core.Services.Commands;

internal static class ParseHelper
{
    private static readonly SearchValues<char> Delimiters = SearchValues.Create([',', ' ', '}']);

    public static long KbToBytes(ReadOnlySpan<char> memLine)
    {
        Span<Range> ranges = stackalloc Range[3];
        var count = memLine.Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
        return count >= 2 && long.TryParse(memLine[ranges[1]], out var kb) ? kb * 1024 : 0;
    }

    public static long ParseSizeWithUnit(ReadOnlySpan<char> value)
    {
        value = value.Trim();
        if (value.IsEmpty || value.IsWhiteSpace())
        {
            return 0;
        }

        // Plain number = KB (df default)
        if (long.TryParse(value, out var plain))
        {
            return plain * 1024;
        }

        // Suffixed: e.g. "12G", "8.3M", "512K", "1T"
        var suffix = char.ToUpperInvariant(value[^1]);
        var numPart = value[..^1];
        if (!double.TryParse(numPart, CultureInfo.InvariantCulture, out var num))
        {
            return 0;
        }

        return suffix switch
        {
            'K' => (long)(num * 1024),
            'M' => (long)(num * 1024 * 1024),
            'G' => (long)(num * 1024 * 1024 * 1024),
            'T' => (long)(num * 1024L * 1024 * 1024 * 1024),
            _ => 0,
        };
    }

    public static (string Name, string? Value) ExtractMValueEntry(ReadOnlySpan<char> trimmed)
    {
        var mValueIdx = trimmed.IndexOf("mValue=");
        var valueSpan = trimmed[(mValueIdx + "mValue=".Length)..];
        var endIdx = valueSpan.IndexOfAny(Delimiters);
        if (endIdx > 0)
        {
            valueSpan = valueSpan[..endIdx];
        }

        var nameStr = "Unknown";
        var mNameIdx = trimmed.IndexOf("mName=");
        if (mNameIdx >= 0)
        {
            var nameSpan = trimmed[(mNameIdx + "mName=".Length)..];
            var nameEnd = nameSpan.IndexOfAny(Delimiters);
            if (nameEnd > 0)
            {
                nameStr = nameSpan[..nameEnd].ToString();
            }
        }

        return (nameStr, valueSpan.ToString());
    }

    public static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
