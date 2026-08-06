using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class RuntimeSettingsRepository
{
    private readonly SettingsManager<RuntimeSettings> _manager = new(SystemPath.RuntimeSettingsFilePath);
    public RuntimeSettings Settings => _manager.Settings;

    public event Action<RuntimeSettings>? OnSettingsChanged;

    public RuntimeSettingsRepository()
    {
        _manager.SettingsChanged += (settings) => OnSettingsChanged?.Invoke(settings);
    }

    public void Load()
    {
        DatabaseMigrationService.Migrate(
            _manager.FilePath,
            DatabaseMigrations.RuntimeSettingsVersion,
            DatabaseMigrations.ApplyRuntimeSettingsMigration);
        _manager.Load();
    }
    public void Update(RuntimeSettings settings) => _manager.Update(settings);
}
