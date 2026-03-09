using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Data.Paths;

public static class SystemPath
{
    public static readonly string SoftwareDataPath = PathUtils.GetSoftwareFolderPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    public static readonly string DatabaseFolderPath = PathUtils.GetDataFolderPath(SoftwareDataPath);
    public static readonly string ImagesFolderPath = PathUtils.GetImagesFolderPath(SoftwareDataPath);
    public static readonly string DefaultItemsFolderPath = PathUtils.GetItemsFolderPath(SoftwareDataPath);
    public static readonly string BackupFolderPath = PathUtils.GetBackupFolderPath(SoftwareDataPath);
    public static readonly string SettingsFolderPath = PathUtils.GetSettingsFolderPath(SoftwareDataPath);
    public static readonly string LogsFolderPath = PathUtils.GetLogsFolderPath(SoftwareDataPath);

    public static readonly string TempFolderPath = PathUtils.GetSoftwareFolderPath(Path.GetTempPath());

    public static readonly string AuthorThumbnailsPath = PathUtils.GetAuthorThumbnailsFolderPath(SoftwareDataPath);
    public static readonly string ItemThumbnailsPath = PathUtils.GetItemThumbnailsFolderPath(SoftwareDataPath);

    public static readonly string ItemDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.Items);
    public static readonly string CommonAvatarDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.CommonAvatars);

    public static readonly string RuntimeSettingsFilePath = Path.Join(SettingsFolderPath, SystemFileName.Settings.Runtime);
    public static readonly string UserPreferencesFilePath = Path.Join(SettingsFolderPath, SystemFileName.Settings.Preferences);

    public static readonly string SchemeFilePath = Path.Join(SettingsFolderPath, SystemFileName.Scheme);
}
