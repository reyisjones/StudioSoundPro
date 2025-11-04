using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StudioSoundPro.UI.Converters;

/// <summary>
/// Converts boolean values to colors for UI styling
/// </summary>
public class BooleanToColorConverter : IValueConverter
{
    public static readonly BooleanToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string colorPair)
            return null;

        var colors = colorPair.Split('|');
        if (colors.Length != 2)
            return null;

        var selectedColor = boolValue ? colors[0] : colors[1];
        
        if (Color.TryParse(selectedColor, out var color))
        {
            return new SolidColorBrush(color);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}