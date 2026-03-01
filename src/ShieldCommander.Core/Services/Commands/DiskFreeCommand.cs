using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class DiskFreeCommand : IAdbShellCommand<DiskFreeInfo?>
{
    public string Name => nameof(DynamicSections.DiskFree);

    public string CommandText => "df -h /data";

    public DiskFreeInfo? Parse(ReadOnlySpan<char> output)
    {
        var lineIndex = 0;
        ReadOnlySpan<char> dataLine = default;

        foreach (var line in output.EnumerateLines())
        {
            if (line.Trim().IsEmpty)
            {
                continue;
            }

            lineIndex++;
            if (lineIndex == 2)
            {
                dataLine = line;
                break;
            }
        }

        if (dataLine.IsEmpty)
        {
            return null;
        }

        Span<Range> cols = stackalloc Range[6];
        var colCount = dataLine.Split(cols, ' ', StringSplitOptions.RemoveEmptyEntries);
        if (colCount < 4)
        {
            return null;
        }

        var totalBytes = ParseHelper.ParseSizeWithUnit(dataLine[cols[1]]);
        if (totalBytes > 0)
        {
            return new DiskFreeInfo(totalBytes);
        }

        return null;
    }

    public void Apply(ReadOnlySpan<char> output, DynamicSections target)
        => target.DiskFree = Parse(output);
}
