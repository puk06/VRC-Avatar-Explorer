using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

public static class DatabaseMigrationService
{
    public static void MigrateDatabase(string filePath, int currentVersion, Func<JsonArray, int, bool> applyMigration)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = JsonNode.Parse(json);
            if (root is null) return;

            JsonObject container;
            JsonArray items;
            int appliedVersion;
            bool wasOldFormat = false;

            if (root is JsonArray oldArray)
            {
                wasOldFormat = true;
                appliedVersion = ReadLegacyVersionFile(filePath);
                items = oldArray;
                container = [];
                container["Items"] = oldArray;
                container["Version"] = appliedVersion;
            }
            else if (root is JsonObject obj && obj.TryGetPropertyValue("Items", out var itemsNode) && itemsNode is JsonArray arr)
            {
                container = obj;
                items = arr;
                appliedVersion = obj["Version"]?.GetValue<int>() ?? 0;
            }
            else
            {
                return;
            }

            var changed = false;
            if (appliedVersion < currentVersion)
            {
                for (int targetVersion = appliedVersion + 1; targetVersion <= currentVersion; targetVersion++)
                {
                    changed |= applyMigration(items, targetVersion);
                }
            }

            if (wasOldFormat || changed)
            {
                var backupPath = BuildBackupPath(filePath, appliedVersion, currentVersion);
                File.Copy(filePath, backupPath, overwrite: true);

                container["Version"] = currentVersion;
                var migratedJson = container.ToJsonString(JsonManager.JsonSerializerOptions);
                File.WriteAllText(filePath, migratedJson);
            }

            DeleteLegacyVersionFile(filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate database: '{filePath}'.", ex);
        }
    }

    public static void MigrateSettings(string filePath, int currentVersion, Func<JsonObject, int, bool> applyMigration)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = JsonNode.Parse(json);
            if (root is not JsonObject settings) return;

            int appliedVersion;
            bool hadVersionField = false;

            if (settings.TryGetPropertyValue("Version", out var versionNode) &&
                versionNode is JsonValue v && v.TryGetValue(out int ver))
            {
                appliedVersion = ver;
                hadVersionField = true;
            }
            else
            {
                appliedVersion = ReadLegacyVersionFile(filePath);
            }

            var changed = false;
            if (appliedVersion < currentVersion)
            {
                for (int targetVersion = appliedVersion + 1; targetVersion <= currentVersion; targetVersion++)
                {
                    changed |= applyMigration(settings, targetVersion);
                }
            }

            if (!hadVersionField || changed)
            {
                var backupPath = BuildBackupPath(filePath, appliedVersion, currentVersion);
                File.Copy(filePath, backupPath, overwrite: true);

                settings["Version"] = currentVersion;
                var migratedJson = settings.ToJsonString(JsonManager.JsonSerializerOptions);
                File.WriteAllText(filePath, migratedJson);
            }

            DeleteLegacyVersionFile(filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate settings: '{filePath}'.", ex);
        }
    }

    private static string BuildBackupPath(string filePath, int fromVersion, int toVersion)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        return Path.Combine(directory, $"{name}.migration-v{fromVersion}-to-{toVersion}.bak{extension}");
    }

    private static int ReadLegacyVersionFile(string filePath)
    {
        var versionFilePath = filePath + ".migration.version";
        if (!File.Exists(versionFilePath)) return 0;

        var text = File.ReadAllText(versionFilePath).Trim();
        return int.TryParse(text, out int version) ? version : 0;
    }

    private static void DeleteLegacyVersionFile(string filePath)
    {
        var versionFilePath = filePath + ".migration.version";
        if (!File.Exists(versionFilePath)) return;

        try
        {
            File.Delete(versionFilePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to delete legacy migration version file: '{versionFilePath}'.", ex);
        }
    }
}
