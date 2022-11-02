using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GeneaGrab.Helpers;

public static class Extensions
{
    public static Bitmap ToImageSource(this SixLabors.ImageSharp.Image image)
    {
        var ms = new MemoryStream();
        image.Save(ms, new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder());
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public static Task<DirectoryInfo> CreateFolder(this DirectoryInfo folder, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(folder);
        var newFolder = new DirectoryInfo(Path.Combine(folder.FullName, GetValidFilename(name.Trim(' '))));
        if(!newFolder.Exists) newFolder.Create();
        return Task.FromResult(newFolder);
    }
    public static async Task<DirectoryInfo> CreateFolder(this Task<DirectoryInfo> folder, string name) => await CreateFolder(await folder.ConfigureAwait(false), name).ConfigureAwait(false);
    public static async Task<DirectoryInfo> CreateFolderPath(this DirectoryInfo folder, string path) => await CreateFolderPath(folder, path.Split(Path.DirectorySeparatorChar)).ConfigureAwait(false);
    public static async Task<DirectoryInfo> CreateFolderPath(this DirectoryInfo folder, params string[] path)
    {
        var f = folder;
        foreach (var dir in path) f = await CreateFolder(f, GetValidFilename(dir)).ConfigureAwait(false);
        return f;
    }

    public static async Task<FileInfo> WriteFile(this Task<DirectoryInfo> folder, string filename, string content) => await WriteFile(await folder.ConfigureAwait(false), filename, content).ConfigureAwait(false);
    public static async Task<FileInfo> WriteFile(this DirectoryInfo folder, string filename, string content)
    {
        var file = new FileInfo(Path.Combine(folder.FullName, GetValidFilename(filename.Trim(' '))));
        await File.WriteAllTextAsync(file.FullName, content);
        return file;
    }

    public static async Task<string?> ReadFile(this Task<DirectoryInfo> folder, string filename) => await ReadFile(await folder.ConfigureAwait(false), filename).ConfigureAwait(false);
    public static async Task<string?> ReadFile(this DirectoryInfo folder, string filename)
    {
        var file = Path.Combine(folder.FullName, GetValidFilename(filename.Trim(' ')));
        return File.Exists(file) ? await File.ReadAllTextAsync(file) : null;
    }
    
    public static string GetValidFilename(string path) => Path.GetInvalidFileNameChars().Aggregate(path, (current, character) => current.Replace(character, '_'));
}