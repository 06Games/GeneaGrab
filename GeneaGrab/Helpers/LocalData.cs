using System;
using System.IO;
using System.Threading.Tasks;
using GeneaGrab.Core.Models;
using GeneaGrab.Services;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace GeneaGrab.Helpers;

public static class LocalData
{
    private static string AppName => "GeneaGrab";

    /// <summary>Application appdata folder</summary>
    /// <returns>
    /// On Windows: %localappdata%\GeneaGrab
    /// On MacOS and Linux: ~/.local/share/GeneaGrab
    /// </returns>
    public static readonly string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
    public static readonly string LogFolder = Path.Combine(AppData, "Logs");
    private static readonly DirectoryInfo RegistriesFolder = new(Path.Combine(AppData, "Registries"));

    private const int ThumbnailSize = 512;

    public static FileInfo? GetFile(Frame page, bool thumbnail = false)
    {
        if (page.Registry is null) return null;
        var folder = RegistriesFolder.CreateFolderPath(page.Registry.ProviderId, page.RegistryId);
        if (thumbnail) folder = folder.CreateFolder(".thumbnails");
        return new FileInfo(Path.Combine(folder.FullName, $"p{page.FrameNumber}.jpg"));
    }


    public static Stream? GetImage(Frame page, bool thumbnail = false)
    {
        try
        {
            var file = GetFile(page, thumbnail);
            return file is null || !file.Exists ? null : file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't get image for {ID} page {Number}", page.RegistryId, page.FrameNumber);
            return null;
        }
    }
    public static async Task<string?> SaveImageAsync(Frame page, Image img, bool thumbnail = false)
    {
        var thumb = await _SaveImageAsync(page, await ToThumbnailAsync(img), true).ConfigureAwait(false);
        var result = thumbnail ? thumb : await _SaveImageAsync(page, img).ConfigureAwait(false);

        await using var db = new DatabaseContext();
        await db.SaveChangesAsync();

        return result;
    }

    public static async Task<Image> ToThumbnailAsync(this Image img) => await Task.Run(() =>
    {
        var wScale = (float)ThumbnailSize / img.Width;
        var hScale = (float)ThumbnailSize / img.Height;
        var scale = Math.Min(wScale, hScale);
        return img.Clone(x => x.Resize((int)(img.Width * scale), (int)(img.Height * scale)));
    });

    private static async Task<string?> _SaveImageAsync(Frame page, Image img, bool thumbnail = false)
    {
        try
        {
            img.Metadata.ExifProfile ??= new ExifProfile();
            img.Metadata.ExifProfile.SetValue(ExifTag.DigitalZoomRatio, new Rational((uint)page.ImageSize, 1));
            if (page.TileSize > 0) img.Metadata.ExifProfile.SetValue(ExifTag.TileWidth, (uint)page.TileSize);
            if (page.Width > 0) img.Metadata.ExifProfile.SetValue(ExifTag.ImageWidth, page.Width.Value);
            if (page.Height > 0) img.Metadata.ExifProfile.SetValue(ExifTag.ImageLength, page.Height.Value);
            if (page.FrameNumber > 0) img.Metadata.ExifProfile.SetValue(ExifTag.ImageNumber, (uint)page.FrameNumber);
            if (string.IsNullOrWhiteSpace(page.Notes)) img.Metadata.ExifProfile.RemoveValue(ExifTag.UserComment);
            else img.Metadata.ExifProfile.SetValue(ExifTag.UserComment, page.Notes);


            var file = GetFile(page, thumbnail);
            if (file is null) return null;
            await using var stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);
            await img.SaveAsJpegAsync(stream).ConfigureAwait(false);
            return file.FullName;
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't save image for {ID} page {Number}", page.RegistryId, page.FrameNumber);
            return page.Registry is null ? null : Path.Combine(RegistriesFolder.FullName, Extensions.GetValidFilename(page.Registry.ProviderId), Extensions.GetValidFilename(page.RegistryId),
                $"p{page.FrameNumber}.jpg");
        }
    }
}
