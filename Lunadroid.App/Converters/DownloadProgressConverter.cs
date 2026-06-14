using System.Globalization;
namespace Lunadroid.App.Converters;
public class DownloadProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double p ? Math.Clamp(p / 100.0, 0.0, 1.0) : 0.0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
