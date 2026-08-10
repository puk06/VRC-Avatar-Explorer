using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.System;

public class SettingsManager<T>(string filePath) where T : class, new()
{
    public event Action<T>? SettingsChanged;

    private T _settings = new();
    private readonly string _filePath = filePath;

    public T Settings => _settings;
    public string FilePath => _filePath;
    public int MigrationVersion { get; set; }

    public void Update(T newSettings)
    {
        _settings = newSettings;
        SettingsChanged?.Invoke(newSettings);
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            Update(new T());
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var root = JsonNode.Parse(json);

            if (root is JsonObject obj)
                MigrationVersion = obj["Version"]?.GetValue<int>() ?? 0;

            var loaded = JsonManager.Deserialize<T>(json);
            if (loaded != null) Update(loaded);
            else Update(new T());
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load settings: '{_filePath}'.", ex);
            Update(new T());
        }
    }

    public void Save()
    {
        try
        {
            FileSystemService.PrepareFileDirectory(_filePath);
            var json = JsonManager.Serialize(_settings);
            var root = JsonNode.Parse(json);
            if (root is JsonObject obj)
            {
                obj["Version"] = MigrationVersion;
                json = obj.ToJsonString(JsonManager.JsonSerializerOptions);
            }
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save settings: '{_filePath}'.", ex);
        }
    }
}
