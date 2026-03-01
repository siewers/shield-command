using System.Globalization;
using Avalonia.Data.Converters;

namespace ShieldCommander.UI.Converters;

internal abstract class NumericFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = value switch
        {
            double.NaN => null,
            double d => d,
            long l => (double)l,
            int i => (double)i,
            _ => (double?)null,
        };
        return number.HasValue ? Format(number.Value, parameter) : "\u2014";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    protected abstract string Format(double value, object? parameter);
}
