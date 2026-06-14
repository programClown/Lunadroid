using System.Globalization;
namespace Lunadroid.App.Converters;
public class PlayProgressConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double progress && values[1] is double duration && duration > 0)
            return Math.Clamp(progress / duration, 0.0, 1.0);
        return 0.0;
    }
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
