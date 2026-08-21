using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System;

public class SettingsManager<T>(string filePath) where T : class, new()
{
    public event Action<T>? SettingsChanged;

    private T _settings = new();

    public T Settings => _settings;
    public string FilePath { get; } = filePath;
    public int MigrationVersion { get; set; }

    public void Update(T newSettings)
    {
        _settings = newSettings;
        SettingsChanged?.Invoke(newSettings);
    }

    public void Load()
    {
        if (!File.Exists(FilePath))
        {
            Update(new T());
            return;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var root = JsonNode.Parse(json);

            if (root is JsonObject obj)
                MigrationVersion = obj["Version"]?.GetValue<int>() ?? 0;

            var loaded = JsonManager.Deserialize<T>(json);
            if (loaded != null) Update(loaded);
            else Update(new T());
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load settings: '{FilePath}'.", ex);
            Update(new T());
        }
    }

    public void Save()
    {
        try
        {
            FileSystemService.PrepareFileDirectory(FilePath);
            var json = JsonManager.Serialize(_settings);
            var root = JsonNode.Parse(json);
            if (root is JsonObject obj)
            {
                obj["Version"] = MigrationVersion;
                json = obj.ToJsonString(JsonManager.JsonSerializerOptions);
            }
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save settings: '{FilePath}'.", ex);
        }
    }
}
