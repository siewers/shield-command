namespace ShieldCommander.UI.Converters;

internal sealed class PercentFormatConverter : NumericFormatConverter
{
    public static readonly PercentFormatConverter Instance = new();

    protected override string Format(double value, object? parameter)
    {
        var fmt = parameter as string ?? "F1";
        return value.ToString(fmt) + "%";
    }
}
