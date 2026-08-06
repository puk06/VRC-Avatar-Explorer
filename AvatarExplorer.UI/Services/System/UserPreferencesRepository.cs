using System;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Data.Paths;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.IO;

namespace AvatarExplorer.UI.Services.System;

public class UserPreferencesRepository
{
    private readonly SettingsManager<UserPreferences> _manager = new(UISystemPath.UserPreferencesFilePath);
    public UserPreferences Settings => _manager.Settings;

    public event Action<UserPreferences>? OnSettingsChanged;

    public UserPreferencesRepository()
    {
        _manager.SettingsChanged += (settings) => OnSettingsChanged?.Invoke(settings);
    }

    public void Load(string? path = null)
    {
        DatabaseMigrationService.Migrate(
            _manager.FilePath,
            UIMigrations.UserPreferencesVersion,
            (root, version) => UIMigrations.ApplyUserPreferencesMigration(root, version, SystemPath.RuntimeSettingsFilePath));
        _manager.Load(path);
    }
    public void Update(UserPreferences settings) => _manager.Update(settings);
}
