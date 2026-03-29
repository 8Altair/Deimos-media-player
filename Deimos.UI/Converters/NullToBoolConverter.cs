using System;
using System.Globalization;
using System.Windows.Data;


namespace Deimos.UI.Converters;

public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("NullToBoolConverter does not support ConvertBack.");
}
