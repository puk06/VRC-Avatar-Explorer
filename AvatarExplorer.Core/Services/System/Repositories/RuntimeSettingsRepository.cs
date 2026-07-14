using System;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class RuntimeSettingsRepository
{
    private readonly SettingsManager<RuntimeSettings> _manager = new(SystemPath.RuntimeSettingsFilePath);
    public RuntimeSettings Settings => _manager.Settings;

    public void Load(string? path = null) => _manager.Load(path);
}
