using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class RuntimeSettingsRepository : SettingsRepositoryBase<RuntimeSettings>
{
    /// <summary>実行時設定（RuntimeSettings）のリポジトリを初期化します。</summary>
    public RuntimeSettingsRepository() : base(SystemPath.RuntimeSettingsFilePath) { }

    /// <summary>実行時設定を読み込み、必要に応じてマイグレーションを適用します。</summary>
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
