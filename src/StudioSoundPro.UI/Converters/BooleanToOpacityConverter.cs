using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StudioSoundPro.UI.Converters;

/// <summary>
/// Converts a boolean to opacity value. True = 1.0, False = 0.5
/// Used to visually dim muted clips/tracks.
/// </summary>
public class BooleanToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If true (muted), return lower opacity. If false (not muted), return full opacity
            return boolValue ? 0.5 : 1.0;
        }
        return 1.0; // Default to full opacity
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
