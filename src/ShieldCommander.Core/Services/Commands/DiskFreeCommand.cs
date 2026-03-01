using ShieldCommander.Core.Models;

namespace ShieldCommander.Core.Services.Commands;

internal sealed class DiskFreeCommand : IAdbShellCommand<DiskFreeInfo?>
{
    public string Name => nameof(DynamicSections.DiskFree);

    public string CommandText => "df -h /data";

    public DiskFreeInfo? Parse(string output)
    {
        var dfLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (dfLines.Length < 2)
        {
            return null;
        }

        var cols = dfLines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (cols.Length < 4)
        {
            return null;
        }

        var totalBytes = ParseHelper.ParseSizeWithUnit(cols[1]);
        if (totalBytes > 0)
        {
            return new DiskFreeInfo(totalBytes);
        }

        return null;
    }

    public void Apply(string output, DynamicSections target)
        => target.DiskFree = Parse(output);
}
