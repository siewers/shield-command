namespace ShieldCommander.UI.Converters;

internal sealed class TemperatureFormatConverter : NumericFormatConverter
{
    public static readonly TemperatureFormatConverter Instance = new();

    protected override string Format(double value, object? parameter)
        => $"{value:F1}\u00b0C";
}
