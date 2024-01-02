using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace GeneaGrab.Helpers;

public class JoinConvertor : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable list || !targetType.IsAssignableFrom(typeof(string))) return null;
        return string.Join(", ", list.Cast<object?>().Select(v => v?.ToString()));
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
