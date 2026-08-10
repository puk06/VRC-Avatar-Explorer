using System;
using System.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.UI.Data.Paths;

public static class UISystemPath
{
    public static readonly string SoftwareDataPath = PathUtils.GetRootPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    public static readonly string SettingsFolderPath = PathUtils.GetSettingsFolderPath(SoftwareDataPath);

    public static readonly string UserPreferencesFilePath = Path.Join(SettingsFolderPath, UISystemFileName.Settings.Preferences);
}
