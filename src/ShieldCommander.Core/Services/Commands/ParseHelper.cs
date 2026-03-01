using System.Globalization;

namespace ShieldCommander.Core.Services.Commands;

internal static class ParseHelper
{
    public static long KbToBytes(string memLine)
    {
        var parts = memLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb * 1024 : 0;
    }

    public static long ParseSizeWithUnit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
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

    public static (string Name, string? Value) ExtractMValueEntry(string trimmed)
    {
        var mValueIdx = trimmed.IndexOf("mValue=", StringComparison.Ordinal);
        var valueStr = trimmed[(mValueIdx + "mValue=".Length)..];
        var endIdx = valueStr.IndexOfAny([',', ' ', '}']);
        if (endIdx > 0)
        {
            valueStr = valueStr[..endIdx];
        }

        var nameStr = "Unknown";
        var mNameIdx = trimmed.IndexOf("mName=", StringComparison.Ordinal);
        if (mNameIdx >= 0)
        {
            var nameVal = trimmed[(mNameIdx + "mName=".Length)..];
            var nameEnd = nameVal.IndexOfAny([',', ' ', '}']);
            if (nameEnd > 0)
            {
                nameStr = nameVal[..nameEnd];
            }
        }

        return (nameStr, valueStr);
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
