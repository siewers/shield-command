using System.Buffers;
using System.Globalization;

namespace ShieldCommander.Core.Services.Commands;

internal static class SpanParsingExtensions
{
    private static readonly SearchValues<char> Delimiters = SearchValues.Create(',', ' ', '}');

    extension(ReadOnlySpan<char> span)
    {
        public long KbToBytes()
        {
            Span<Range> ranges = stackalloc Range[3];
            var count = span.Split(ranges, ' ', StringSplitOptions.RemoveEmptyEntries);
            return count >= 2 && long.TryParse(span[ranges[1]], out var kb) ? kb * 1024 : 0;
        }

        public long ParseSizeWithUnit()
        {
            span = span.Trim();
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                return 0;
            }

            // Plain number = KB (df default)
            if (long.TryParse(span, out var plain))
            {
                return plain * 1024;
            }

            // Suffixed: e.g. "12G", "8.3M", "512K", "1T"
            var suffix = char.ToUpperInvariant(span[^1]);
            var numPart = span[..^1];
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

        public void ExtractDumpsysFields(out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            var mValueIdx = span.IndexOf("mValue=");
            value = span[(mValueIdx + "mValue=".Length)..];
            var endIdx = value.IndexOfAny(Delimiters);
            if (endIdx > 0)
            {
                value = value[..endIdx];
            }

            name = "Unknown";
            var mNameIdx = span.IndexOf("mName=");
            if (mNameIdx >= 0)
            {
                name = span[(mNameIdx + "mName=".Length)..];
                var nameEnd = name.IndexOfAny(Delimiters);
                if (nameEnd > 0)
                {
                    name = name[..nameEnd];
                }
            }
        }

        public bool IsAllDigits()
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
}
