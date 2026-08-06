using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Data.Paths;

public static class SystemPath
{
    public static readonly string SoftwareDataPath = PathUtils.GetRootPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    public static readonly string DatabaseFolderPath = PathUtils.GetDatabaseFolderPath(SoftwareDataPath);
    public static readonly string ImagesFolderPath = PathUtils.GetImagesFolderPath(SoftwareDataPath);
    public static readonly string ItemThumbnailsFolderPath = PathUtils.GetItemThumbnailsFolderPath(SoftwareDataPath);
    public static readonly string DefaultItemsFolderPath = PathUtils.GetItemsFolderPath(SoftwareDataPath);
    public static readonly string BackupFolderPath = PathUtils.GetBackupFolderPath(SoftwareDataPath);
    public static readonly string SettingsFolderPath = PathUtils.GetSettingsFolderPath(SoftwareDataPath);
    public static readonly string LogsFolderPath = PathUtils.GetLogsFolderPath(SoftwareDataPath);

    public static readonly string TempFolderPath = PathUtils.GetRootPath(Path.GetTempPath()); // {TempFolder}/Avatar Explorer V2

    public static readonly string ItemDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.Items);
    public static readonly string ItemDatabaseMigrationVersionPath = Path.Join(DatabaseFolderPath, SystemFileName.Database.ItemsDatabaseMigrationVersion);
    public static readonly string CommonAvatarDatabaseMigrationVersionPath = Path.Join(DatabaseFolderPath, SystemFileName.Database.CommonAvatarsDatabaseMigrationVersion);
    public static readonly string BulkImportPresetDatabaseMigrationVersionPath = Path.Join(DatabaseFolderPath, SystemFileName.Database.BulkImportPresetsDatabaseMigrationVersion);
    public static readonly string TempAvatarsDatabaseMigrationVersionPath = Path.Join(DatabaseFolderPath, SystemFileName.Database.TempAvatarsDatabaseMigrationVersion);
    public static readonly string CommonAvatarDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.CommonAvatars);
    public static readonly string BulkImportPresetDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.BulkImportPresets);
    public static readonly string TempAvatarsDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.TempAvatars);

    public static readonly string RuntimeSettingsFilePath = Path.Join(SettingsFolderPath, SystemFileName.Settings.Runtime);
    public static readonly string RuntimeSettingsMigrationVersionPath = Path.Join(SettingsFolderPath, SystemFileName.Settings.RuntimeDatabaseMigrationVersion);
    public static readonly string UserPreferencesFilePath = Path.Join(SettingsFolderPath, SystemFileName.Settings.Preferences); // TODO: システムパスにこれがあるのはおかしい。UIのExtensionとかにするべき
    public static readonly string UserPreferencesMigrationVersionPath = Path.Join(SettingsFolderPath, SystemFileName.Settings.PreferencesDatabaseMigrationVersion);

    public static readonly string SchemeFilePath = Path.Join(SettingsFolderPath, SystemFileName.Scheme);
}
