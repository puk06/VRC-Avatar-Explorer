namespace AvatarExplorer.Core.Data.Paths;

public static class SystemFileName
{
    public static class Database
    {
        public const string Items = "items.json";
        public const string ItemsDatabaseMigrationVersion = "items.json.migration.version";
        public const string CommonAvatars = "commonAvatars.json";
        public const string CommonAvatarsDatabaseMigrationVersion = "commonAvatars.json.migration.version";
        public const string BulkImportPresets = "bulkImportPresets.json";
        public const string BulkImportPresetsDatabaseMigrationVersion = "bulkImportPresets.json.migration.version";
        public const string TempAvatars = "tempAvatars.json";
        public const string TempAvatarsDatabaseMigrationVersion = "tempAvatars.json.migration.version";
    }

    public static class Settings
    {
        public const string Runtime = "runtimeSettings.json";
        public const string RuntimeDatabaseMigrationVersion = "runtimeSettings.json.migration.version";
    }

    public const string Scheme = "VRCAESCHEME";

    public const string Lisence = "LISENCE.txt";
    public const string ThirdPartyLisences = "THIRD_PARTY_LICENSES.txt";
}
