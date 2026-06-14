using System.Globalization;
using Lunadroid.Core.Helpers;
namespace Lunadroid.App.Converters;
public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is long b ? RelativeTimeHelper.FormatFileSize(b) : "0 B";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
