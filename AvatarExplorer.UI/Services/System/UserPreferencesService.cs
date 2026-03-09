using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.UI.Models.Settings;

namespace AvatarExplorer.UI.Services.System;

internal static class UserPreferencesService
{
    internal static UserPreferences Load(string path)
    {
        return FileSystemService.DeserializeClass<UserPreferences>(path).Value ?? new();
    }

    internal static void Save(UserPreferences userPreferences)
    {
        FileSystemService.SerializeClass(userPreferences, SystemPath.UserPreferencesFilePath);
    }
}
