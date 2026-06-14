using System.Globalization;
using Lunadroid.Core.Helpers;
namespace Lunadroid.App.Converters;
public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dt ? RelativeTimeHelper.ToRelativeTime(dt) : string.Empty;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
