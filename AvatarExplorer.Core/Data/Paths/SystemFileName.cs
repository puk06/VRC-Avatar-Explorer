namespace AvatarExplorer.Core.Data.Paths;

public static class SystemFileName
{
    public static class Database
    {
        public const string Items = "items.json";
        public const string ItemsDatabaseMigrationVersion = "items.json.migration.version";
        public const string CommonAvatars = "commonAvatars.json";
        public const string BulkImportPresets = "bulkImportPresets.json";
        public const string TempAvatars = "tempAvatars.json";
    }

    public static class Settings
    {
        public const string Runtime = "runtimeSettings.json";
        public const string Preferences = "preferenceSettings.json";
    }

    public const string Scheme = "VRCAESCHEME";
}
