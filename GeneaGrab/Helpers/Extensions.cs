using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media.Imaging;
using GeneaGrab.Core.Models;

namespace GeneaGrab.Helpers;

public static class Extensions
{
    public static Bitmap ToBitmap(this Stream stream, bool close = true)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var bitmap = new Bitmap(stream);
        if (close) stream.Close();
        return bitmap;
    }

    public static DirectoryInfo CreateFolder(this DirectoryInfo folder, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return folder;
        var newFolder = new DirectoryInfo(Path.Combine(folder.FullName, GetValidFilename(name.Trim(' '))));
        if (!newFolder.Exists) newFolder.Create();
        return newFolder;
    }
    public static DirectoryInfo CreateFolderPath(this DirectoryInfo folder, params string[] path)
        => path.Aggregate(folder, (current, dir) => CreateFolder(current, GetValidFilename(dir)));

    public static string GetValidFilename(string path) => Path.GetInvalidFileNameChars().Aggregate(path, (current, character) => current.Replace(character, '_'));


    public static string GetDescription(this Registry registry)
    {
        var desc = new StringBuilder();

        // Type
        desc.Append(string.Join(", ", registry.Types.Select(t =>
        {
            var type = Enum.GetName(typeof(RegistryType), t);
            return ResourceExtensions.GetLocalized($"Registry/Type/{type}") ?? type;
        })));

        // Dates
        if (registry.From != null || registry.To != null)
        {
            desc.Append(" (");
            desc.Append(registry.From == registry.To ? registry.From!.ToString() : $"{registry.From ?? "?"} - {registry.To ?? "?"}");
            desc.Append(')');
        }

        // Title
        if (!string.IsNullOrEmpty(registry.Title)) desc.Append($"\n{registry.Title}");
        else if (!string.IsNullOrEmpty(registry.Notes)) desc.Append($"\n{registry.Notes.Split('\n').FirstOrDefault()}");

        if (!string.IsNullOrEmpty(registry.Subtitle)) desc.Append($" ({registry.Subtitle})");
        if (!string.IsNullOrEmpty(registry.Author)) desc.Append($"\n{registry.Author}");

        return desc.ToString();
    }
}
