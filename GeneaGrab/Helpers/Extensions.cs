using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

public static class Extensions
{
    public static BitmapImage ToImageSource(this SixLabors.ImageSharp.Image image)
    {
        var img = new BitmapImage();
        if (image is null) return img;
        InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream();
        image.Save(ms.AsStreamForWrite(), new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder());
        ms.Seek(0);
        img.SetSource(ms);
        return img;
    }

    public static async Task<Windows.Storage.StorageFolder> CreateFolder(this Windows.Storage.StorageFolder folder, string name)
    {
        if (folder is null || string.IsNullOrWhiteSpace(name)) return folder;
        else return await folder.CreateFolderAsync(GetValidFilename(name.Trim(' ')), Windows.Storage.CreationCollisionOption.OpenIfExists);
    }
    public static async Task<Windows.Storage.StorageFolder> CreateFolder(this Task<Windows.Storage.StorageFolder> folder, string name) => await CreateFolder(await folder.ConfigureAwait(false), name).ConfigureAwait(false);
    public static async Task<Windows.Storage.StorageFolder> CreateFolderPath(this Windows.Storage.StorageFolder folder, string path) => await CreateFolderPath(folder, path.Split(Path.DirectorySeparatorChar)).ConfigureAwait(false);
    public static async Task<Windows.Storage.StorageFolder> CreateFolderPath(this Windows.Storage.StorageFolder folder, params string[] path)
    {
        Windows.Storage.StorageFolder f = folder;
        foreach (var dir in path) f = await CreateFolder(f, GetValidFilename(dir)).ConfigureAwait(false);
        return f;
    }

    public static async Task<Windows.Storage.StorageFile> WriteFile(this Task<Windows.Storage.StorageFolder> folder, string filename, string content) => await WriteFile(await folder.ConfigureAwait(false), filename, content).ConfigureAwait(false);
    public static async Task<Windows.Storage.StorageFile> WriteFile(this Windows.Storage.StorageFolder folder, string filename, string content)
    {
        var file = await folder.CreateFileAsync(GetValidFilename(filename.Trim(' ')), Windows.Storage.CreationCollisionOption.OpenIfExists);
        File.WriteAllText(file.Path, content);
        return file;
    }

    public static async Task<string> ReadFile(this Task<Windows.Storage.StorageFolder> folder, string filename) => await ReadFile(await folder.ConfigureAwait(false), filename);
    public static async Task<string> ReadFile(this Windows.Storage.StorageFolder folder, string filename)
    {
        var file = await folder.CreateFileAsync(GetValidFilename(filename.Trim(' ')), Windows.Storage.CreationCollisionOption.OpenIfExists);
        return File.ReadAllText(file.Path);
    }



    public static string GetValidFilename(string path)
    {
        foreach (var _char in Path.GetInvalidFileNameChars()) path = path.Replace(_char, '_');
        return path;
    }
}
