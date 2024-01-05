using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GeneaGrab.Strings;

namespace GeneaGrab.Helpers;

public static class ResourceExtensions
{
    public enum Resource { Core, UI }

    public static string? GetLocalized(string key, Resource src = Resource.Core) => GetLocalized(key, CultureInfo.CurrentUICulture, src);
    public static string? GetLocalized(string key, CultureInfo culture, Resource src = Resource.Core)
    {
        var manager = src switch
        {
            Resource.Core => Strings.Core.ResourceManager,
            Resource.UI => UI.ResourceManager,
            _ => throw new ArgumentOutOfRangeException(nameof(src), src, null)
        };

        return manager.GetString(key.Replace('/', '.'), culture);
    }
}
public class ResourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!targetType.IsAssignableFrom(typeof(string))) throw new NotSupportedException();
        return GetLocalized(value?.ToString(), parameter as string, culture);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();

    public static string? GetLocalized(string? value, string? param, CultureInfo culture)
    {
        if (value is null) return null;
        var res = ResourceExtensions.Resource.UI;
        var cat = string.Empty;
        if (param is null) return ResourceExtensions.GetLocalized($"{cat}.{value}", culture, res);
        if (param.Contains('@'))
        {
            var parameters = param.Split('@');
            if (parameters.Length != 2) return ResourceExtensions.GetLocalized($"{cat}.{value}", culture, res);
            if (Enum.TryParse(parameters[0], out ResourceExtensions.Resource parsedRes)) res = parsedRes;
            cat = parameters[1];
        }
        else cat = param;
        return ResourceExtensions.GetLocalized($"{cat}.{value}", culture, res);
    }
}
