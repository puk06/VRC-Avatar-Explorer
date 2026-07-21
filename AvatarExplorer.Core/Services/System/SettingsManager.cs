using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System;

public class SettingsManager<T>(string filePath) where T : class, new()
{
    public event Action<T>? SettingsChanged;

    private T _settings = new();
    private readonly string _filePath = filePath;

    public T Settings => _settings;
    public string FilePath => _filePath;

    public void Update(T newSettings, bool save = true)
    {
        _settings = newSettings;
        SettingsChanged?.Invoke(newSettings);
        if (save) Save();
    }

    public void Load(string? path = null)
    {
        var loadedSettings = JsonFileManager<T>.Load(path ?? _filePath);
        if (loadedSettings != null) Update(loadedSettings, false);
        else Update(new T(), false);
    }

    public void Save() => JsonFileManager<T>.Save(_settings, _filePath);
}
