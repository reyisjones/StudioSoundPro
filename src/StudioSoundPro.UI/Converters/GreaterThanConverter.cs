using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StudioSoundPro.UI.Converters;

/// <summary>
/// Compares a numeric value against a parameter threshold.
/// Returns true if value > parameter, false otherwise.
/// Used for conditional visibility based on width/height.
/// </summary>
public class GreaterThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr)
        {
            if (double.TryParse(paramStr, out double threshold))
            {
                return doubleValue > threshold;
            }
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
