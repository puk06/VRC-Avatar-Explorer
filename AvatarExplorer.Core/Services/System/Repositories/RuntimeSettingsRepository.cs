using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class RuntimeSettingsRepository : SettingsRepositoryBase<RuntimeSettings>
{
    public RuntimeSettingsRepository() : base(SystemPath.RuntimeSettingsFilePath) { }

    public override void Load()
    {
        DatabaseMigrationService.MigrateSettings(
            Manager.FilePath,
            DatabaseMigrations.RuntimeSettingsVersion,
            DatabaseMigrations.ApplyRuntimeSettingsMigration);
        Manager.Load();
        Manager.MigrationVersion = DatabaseMigrations.RuntimeSettingsVersion;
    }
}
