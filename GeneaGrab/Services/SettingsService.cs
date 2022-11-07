using System.IO;
using GeneaGrab.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeneaGrab.Services;

public static class SettingsService
{
    private static readonly string FilePath = Path.Combine(LocalData.AppData, "Settings.json");
    
    private static SettingsData? settingsData;
    public static SettingsData SettingsData
    {
        get {
            if (settingsData is not null) return settingsData;
            if (!File.Exists(FilePath)) return settingsData = new SettingsData();
            
            var data = File.ReadAllText(FilePath);
            return settingsData = JsonConvert.DeserializeObject<SettingsData>(data) ?? new SettingsData();
        }
        set
        {
            if (Equals(settingsData, value)) return;
            settingsData = value;
            Save();
        }
    }

    public static void Save()
    {
        var data = JsonConvert.SerializeObject(settingsData, Formatting.Indented);
        File.WriteAllText(FilePath, data);
    }
}

public class SettingsData
{
    [JsonProperty("Theme"), JsonConverter(typeof(StringEnumConverter))] private Theme theme;
    [JsonIgnore] public Theme Theme { get => theme; set => Set(ref theme, value); }
    
    
    private void Set<T>(ref T storage, T value)
    {
        if (Equals(storage, value)) return;
        storage = value;
        SettingsService.Save();
    }
}
