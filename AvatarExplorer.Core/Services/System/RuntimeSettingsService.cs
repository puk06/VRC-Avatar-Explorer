using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System;

internal static class RuntimeSettingsService
{
    internal static RuntimeSettings Load(string path)
    {
        return FileSystemService.DeserializeClass<RuntimeSettings>(path).Value;
    }

    internal static void Save(RuntimeSettings settings)
    {
        FileSystemService.SerializeClass(settings, SystemPath.RuntimeSettingsFilePath);
    }
}
