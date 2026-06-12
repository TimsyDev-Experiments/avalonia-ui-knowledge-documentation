using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DemoApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDone)
            return isDone ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
