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
        else return await folder.CreateFolderAsync(name.Trim(' '), Windows.Storage.CreationCollisionOption.OpenIfExists);
    }
    public static async Task<Windows.Storage.StorageFolder> CreateFolder(this Task<Windows.Storage.StorageFolder> folder, string name) => await CreateFolder(await folder, name);
    public static async Task<Windows.Storage.StorageFolder> CreateFolderPath(this Windows.Storage.StorageFolder folder, string path) => await CreateFolderPath(folder, path.Split(Path.DirectorySeparatorChar));
    public static async Task<Windows.Storage.StorageFolder> CreateFolderPath(this Windows.Storage.StorageFolder folder, params string[] path)
    {
        Windows.Storage.StorageFolder f = folder;
        foreach (var dir in path) f = await CreateFolder(f, dir);
        return f;
    }

    public static async Task<Windows.Storage.StorageFile> WriteFile(this Task<Windows.Storage.StorageFolder> folder, string filename, string content) => await WriteFile(await folder, filename, content);
    public static async Task<Windows.Storage.StorageFile> WriteFile(this Windows.Storage.StorageFolder folder, string filename, string content)
    {
        var file = await folder.CreateFileAsync(filename.Trim(' '), Windows.Storage.CreationCollisionOption.OpenIfExists);
        File.WriteAllText(file.Path, content);
        return file;
    }

    public static async Task<string> ReadFile(this Task<Windows.Storage.StorageFolder> folder, string filename) => await ReadFile(await folder, filename);
    public static async Task<string> ReadFile(this Windows.Storage.StorageFolder folder, string filename)
    {
        var file = await folder.CreateFileAsync(filename.Trim(' '), Windows.Storage.CreationCollisionOption.OpenIfExists);
        return File.ReadAllText(file.Path);
    }
}
