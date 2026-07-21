using System;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Models.Settings;

namespace AvatarExplorer.UI.Services.System;

public class UserPreferencesRepository
{
    private readonly SettingsManager<UserPreferences> _manager = new(SystemPath.UserPreferencesFilePath);
    public UserPreferences Settings => _manager.Settings;

    public event Action<UserPreferences>? OnSettingsChanged;

    public UserPreferencesRepository()
    {
        _manager.SettingsChanged += (settings) => OnSettingsChanged?.Invoke(settings);
    }

    public void Load(string? path = null) => _manager.Load(path);
    public void Update(UserPreferences settings) => _manager.Update(settings);
}
