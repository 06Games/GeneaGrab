using System;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace GeneaGrab.Helpers;

/// <summary>
/// <para>
/// Converts a string path to a bitmap asset.
/// </para>
/// <para>
/// The asset must be in the same assembly as the program. If it isn't,
/// specify "avares://assemblynamehere/" in front of the path to the asset.
/// </para>
/// </summary>
public class BitmapAssetValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        if (value is not string rawUri || !targetType.IsAssignableFrom(typeof(Bitmap))) throw new NotSupportedException();

        if (parameter is string format) rawUri = string.Format(format, rawUri);

        var uri = new Uri(rawUri.StartsWith("avares://") ? rawUri : $"avares://{Assembly.GetEntryAssembly()?.GetName().Name}{rawUri}");
        if (!AssetLoader.Exists(uri)) return null;
        var asset = AssetLoader.Open(uri);
        return new Bitmap(asset);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
