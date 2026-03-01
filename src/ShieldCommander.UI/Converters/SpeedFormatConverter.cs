using ShieldCommander.UI.Formatters;

namespace ShieldCommander.UI.Converters;

internal sealed class SpeedFormatConverter : NumericFormatConverter
{
    public static readonly SpeedFormatConverter Instance = new();

    protected override string Format(double value, object? parameter)
        => UnitFormatter.Speed((long)value);
}
