using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

internal static class ItemDatabaseMigrationService
{
    private const int CurrentMigrationVersion = 2;

    private const string LegacyThumbnailKey = "ThumbnmailFileName";
    private const string ThumbnailKey = "ThumbnailFileName";

    private const int ItemTypeOffset = 1; // ItemType enum values were shifted up by 1 in version 2

    internal static void Migrate(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            int appliedVersion = ReadAppliedMigrationVersion(filePath);
            if (appliedVersion >= CurrentMigrationVersion) return;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = JsonNode.Parse(json);
            if (root is not JsonArray items) return;

            var changed = false;
            for (int targetVersion = appliedVersion + 1; targetVersion <= CurrentMigrationVersion; targetVersion++)
            {
                changed |= ApplyMigration(items, targetVersion);
            }

            if (changed)
            {
                var backupPath = BuildBackupPath(filePath, appliedVersion, CurrentMigrationVersion);
                File.Copy(filePath, backupPath, overwrite: true);

                var migratedJson = root.ToJsonString(JsonManager.JsonSerializerOptions);
                File.WriteAllText(filePath, migratedJson);
            }

            WriteAppliedMigrationVersion(filePath, CurrentMigrationVersion);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate item database: '{filePath}'.", ex);
        }
    }

    internal static void MarkCurrentVersion(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return;
            WriteAppliedMigrationVersion(filePath, CurrentMigrationVersion);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to mark item database migration version: '{filePath}'.", ex);
        }
    }

    private static bool ApplyMigration(JsonArray items, int targetVersion)
    {
        return targetVersion switch
        {
            1 => MigrateV1RenameThumbnailKey(items),
            2 => MigrateV2ItemTypeOffset(items),
            _ => false
        };
    }

    private static bool MigrateV1RenameThumbnailKey(JsonArray items)
    {
        var changed = false;

        foreach (var itemNode in items)
        {
            if (itemNode is not JsonObject itemObject) continue;

            var hasLegacy = itemObject.ContainsKey(LegacyThumbnailKey);
            var hasCurrent = itemObject.ContainsKey(ThumbnailKey);
            if (!hasLegacy) continue;

            var legacyValue = itemObject[LegacyThumbnailKey];
            if (!hasCurrent) itemObject[ThumbnailKey] = legacyValue?.DeepClone();

            itemObject.Remove(LegacyThumbnailKey);
            changed = true;
        }

        return changed;
    }

    private static bool MigrateV2ItemTypeOffset(JsonArray items)
    {
        var changed = false;

        foreach (var itemNode in items)
        {
            if (itemNode is not JsonObject itemObject) continue;
            if (!itemObject.ContainsKey("Type")) continue;

            var typeNode = itemObject["Type"];
            if (typeNode is not JsonValue typeValue || !typeValue.TryGetValue(out int typeInt)) continue;

            int migratedTypeInt = typeInt + ItemTypeOffset;
            itemObject["Type"] = migratedTypeInt;
            changed = true;
        }

        return changed;
    }

    private static string BuildBackupPath(string filePath, int fromVersion, int toVersion)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        return Path.Combine(directory, $"{name}.migration-v{fromVersion}-to-v{toVersion}.bak{extension}");
    }

    internal static int ReadAppliedMigrationVersion(string filePath)
    {
        var versionFilePath = BuildVersionFilePath(filePath);
        if (!File.Exists(versionFilePath)) return 0;

        var text = File.ReadAllText(versionFilePath).Trim();
        return int.TryParse(text, out int version) ? version : 0;
    }

    internal static void WriteAppliedMigrationVersion(string filePath, int version)
    {
        var versionFilePath = BuildVersionFilePath(filePath);
        File.WriteAllText(versionFilePath, version.ToString());
    }

    private static string BuildVersionFilePath(string filePath)
    {
        return filePath + ".migration.version";
    }
}
