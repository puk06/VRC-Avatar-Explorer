using System.Text.Json.Nodes;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// データベースファイル（JSON）に対して、現在のバージョンまでのマイグレーションを実行し、バックアップを作成する静的クラスです。
/// </summary>
public static class DatabaseMigrationService
{
    /// <summary>
    /// 指定したデータベースファイルを読み込み、現在のバージョンまでマイグレーションを適用して保存します。
    /// </summary>
    /// <param name="filePath">マイグレーション対象のデータベースファイルのパス。</param>
    /// <param name="currentVersion">適用する最新のスキーマバージョン。</param>
    /// <param name="applyMigration">個々のバージョン番号に対してマイグレーションを実行するコールバック。</param>
    public static void MigrateDatabase(string filePath, int currentVersion, Func<JsonArray, int, bool> applyMigration)
    {
        try
        {
            if (!TryReadJson(filePath, out var root)) return;

            JsonObject container;
            JsonArray items;
            int appliedVersion;
            bool wasOldFormat;

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
                wasOldFormat = false;
                container = obj;
                items = arr;
                appliedVersion = obj["Version"]?.GetValue<int>() ?? 0;
            }
            else
            {
                return;
            }

            var changed = RunMigrations(items, appliedVersion, currentVersion, applyMigration);

            if (wasOldFormat || changed)
                WriteWithVersion(filePath, container, appliedVersion, currentVersion);

            DeleteLegacyVersionFile(filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate database: '{filePath}'.", ex);
        }
    }

    /// <summary>
    /// 指定した設定ファイル（JSON）を読み込み、現在のバージョンまでマイグレーションを適用して保存します。
    /// </summary>
    /// <param name="filePath">マイグレーション対象の設定ファイルのパス。</param>
    /// <param name="currentVersion">適用する最新のスキーマバージョン。</param>
    /// <param name="applyMigration">個々のバージョン番号に対してマイグレーションを実行するコールバック。</param>
    public static void MigrateSettings(string filePath, int currentVersion, Func<JsonObject, int, bool> applyMigration)
    {
        try
        {
            if (!TryReadJson(filePath, out var root) || root is not JsonObject settings) return;

            int appliedVersion;
            bool hadVersionField;

            if (settings.TryGetPropertyValue("Version", out var versionNode) &&
                versionNode is JsonValue v && v.TryGetValue(out int ver))
            {
                appliedVersion = ver;
                hadVersionField = true;
            }
            else
            {
                appliedVersion = ReadLegacyVersionFile(filePath);
                hadVersionField = false;
            }

            var changed = RunMigrations(settings, appliedVersion, currentVersion, applyMigration);

            if (!hadVersionField || changed)
                WriteWithVersion(filePath, settings, appliedVersion, currentVersion);

            DeleteLegacyVersionFile(filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to migrate settings: '{filePath}'.", ex);
        }
    }

    private static bool TryReadJson(string filePath, out JsonNode? root)
    {
        root = null;
        if (!File.Exists(filePath)) return false;

        var json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json)) return false;

        root = JsonNode.Parse(json);
        return root != null;
    }

    private static bool RunMigrations<T>(T target, int fromVersion, int toVersion, Func<T, int, bool> applyMigration)
    {
        var changed = false;
        if (fromVersion < toVersion)
        {
            for (int v = fromVersion + 1; v <= toVersion; v++)
                changed |= applyMigration(target, v);
        }
        return changed;
    }

    private static void WriteWithVersion(string filePath, JsonObject node, int fromVersion, int toVersion)
    {
        var backupPath = BuildBackupPath(filePath, fromVersion, toVersion);
        File.Copy(filePath, backupPath, overwrite: true);
        node["Version"] = toVersion;
        File.WriteAllText(filePath, node.ToJsonString(JsonManager.JsonSerializerOptions));
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
