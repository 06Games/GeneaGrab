using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GeneaGrab.Helpers;

public static class Extensions
{
    public static Bitmap ToBitmap(this Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        return new Bitmap(stream);
    }
    
    public static DirectoryInfo CreateFolder(this DirectoryInfo folder, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return folder;
        var newFolder = new DirectoryInfo(Path.Combine(folder.FullName, GetValidFilename(name.Trim(' '))));
        if(!newFolder.Exists) newFolder.Create();
        return newFolder;
    }
    public static DirectoryInfo CreateFolderPath(this DirectoryInfo folder, params string[] path)
        => path.Aggregate(folder, (current, dir) => CreateFolder(current, GetValidFilename(dir)));
    public static async Task<FileInfo> WriteFileAsync(this DirectoryInfo folder, string filename, string content)
    {
        var file = new FileInfo(Path.Combine(folder.FullName, GetValidFilename(filename.Trim(' '))));
        await File.WriteAllTextAsync(file.FullName, content);
        return file;
    }
    
    public static string GetValidFilename(string path) => Path.GetInvalidFileNameChars().Aggregate(path, (current, character) => current.Replace(character, '_'));
}