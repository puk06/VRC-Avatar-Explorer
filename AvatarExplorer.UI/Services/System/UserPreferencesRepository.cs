using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Data.Paths;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.IO;

namespace AvatarExplorer.UI.Services.System;

public class UserPreferencesRepository : SettingsRepositoryBase<UserPreferences>
{
    public UserPreferencesRepository() : base(UISystemPath.UserPreferencesFilePath) { }

    public override void Load()
    {
        DatabaseMigrationService.Migrate(
            Manager.FilePath,
            UIMigrations.UserPreferencesVersion,
            (root, version) => UIMigrations.ApplyUserPreferencesMigration(root, version, SystemPath.RuntimeSettingsFilePath));
        Manager.Load();
    }
}
