using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;

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
            Resource.UI => Strings.UI.ResourceManager,
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
        Debug.Assert(value != null, nameof(value) + " != null");

        var res = ResourceExtensions.Resource.Core;
        var cat = string.Empty;
        if (parameter is string param)
        {
            if (param.Contains('@'))
            {
                var parameters = param.Split('@');
                if (parameters.Length == 2)
                {
                    if (Enum.TryParse(parameters[0], out ResourceExtensions.Resource parsedRes)) res = parsedRes;
                    cat = parameters[1];
                }
            }
            else cat = param;
        }
        return ResourceExtensions.GetLocalized($"{cat}.{value}", culture, res);
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
