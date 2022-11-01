using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GeneaGrab.Helpers;

public static class ResourceExtensions
{
    public static string? GetLocalized(string key) => GetLocalized(key, CultureInfo.CurrentUICulture);
    public static string? GetLocalized(string key, CultureInfo culture) 
        => Strings.Core.ResourceManager.GetString(key.Replace('/', '.'), culture);
}

public class ResourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!targetType.IsAssignableFrom(typeof(string))) throw new NotSupportedException();
        Debug.Assert(value != null, nameof(value) + " != null");
        if (parameter != null) value = $"{parameter}.{value}";
        return ResourceExtensions.GetLocalized(value.ToString()!, culture);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
