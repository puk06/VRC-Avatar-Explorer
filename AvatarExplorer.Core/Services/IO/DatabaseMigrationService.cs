using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

public static class DatabaseMigrationService
{
    public static void Migrate(string filePath, int currentVersion, Func<JsonNode, int, bool> applyMigration)
    {
        try
        {
            if (!File.Exists(filePath)) return;

            int appliedVersion = ReadAppliedMigrationVersion(filePath);
            if (appliedVersion >= currentVersion) return;

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var root = JsonNode.Parse(json);
            if (root is null) return;

            var changed = false;
            for (int targetVersion = appliedVersion + 1; targetVersion <= currentVersion; targetVersion++)
            {
                changed |= applyMigration(root, targetVersion);
            }

            if (changed)
            {
                var backupPath = BuildBackupPath(filePath, appliedVersion, currentVersion);
                File.Copy(filePath, backupPath, overwrite: true);

                var migratedJson = root.ToJsonString(JsonManager.JsonSerializerOptions);
                File.WriteAllText(filePath, migratedJson);
            }

            WriteAppliedMigrationVersion(filePath, currentVersion);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate database: '{filePath}'.", ex);
        }
    }

    internal static void MarkCurrentVersion(string filePath, int version)
    {
        try
        {
            if (!File.Exists(filePath)) return;
            WriteAppliedMigrationVersion(filePath, version);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to mark migration version: '{filePath}'.", ex);
        }
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
