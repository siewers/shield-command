namespace ShieldCommander.UI.Formatters;

internal static class UnitFormatter
{
    private static readonly string[] ByteUnits = ["B", "KB", "MB", "GB", "TB", "PB"];
    private static readonly string[] SpeedUnits = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s"];
    private static readonly string[] CountUnits = ["", "K", "M", "B", "T"];

    internal static string Bytes(long value) => Format(value, 1024, ByteUnits);
    internal static string? Bytes(long? value) => value.HasValue ? Bytes(value.Value) : null;
    internal static string Speed(long value) => Format(value, 1024, SpeedUnits);
    internal static string Count(long value) => Format(value, 1000, CountUnits);

    private static string Format(double value, double divisor, string[] units)
    {
        var order = 0;
        while (value >= divisor && order < units.Length - 1)
        {
            order++;
            value /= divisor;
        }

        if (units[order].Length == 0)
        {
            return ((long)value).ToString("N0");
        }

        return $"{value:0.##} {units[order]}";
    }
}
