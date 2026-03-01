using ShieldCommander.UI.Formatters;

namespace ShieldCommander.UI.Converters;

internal sealed class CountFormatConverter : NumericFormatConverter
{
    public static readonly CountFormatConverter Instance = new();

    protected override string Format(double value, object? parameter)
        => UnitFormatter.Count((long)value);
}
