using ShieldCommander.UI.Formatters;

namespace ShieldCommander.UI.Converters;

internal sealed class ByteFormatConverter : NumericFormatConverter
{
    public static readonly ByteFormatConverter Instance = new();

    protected override string Format(double value, object? parameter)
        => UnitFormatter.Bytes((long)value);
}
