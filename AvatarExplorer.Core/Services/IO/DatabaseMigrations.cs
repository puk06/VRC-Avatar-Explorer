using System.Text.Json.Nodes;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// データベースの JSON データに対して、スキーマバージョンに応じたマイグレーション（互換性維持のための変換）を行う静的クラスです。
/// </summary>
public static class DatabaseMigrations
{
    /// <summary>アイテムデータベースの現在のスキーマバージョン。</summary>
    public const int ItemVersion = 4;
    /// <summary>共通素体データベースの現在のスキーマバージョン。</summary>
    public const int CommonAvatarVersion = 1;
    /// <summary>一括インポートプリセットデータベースの現在のスキーマバージョン。</summary>
    public const int BulkImportPresetVersion = 1;
    /// <summary>ランタイム設定の現在のスキーマバージョン。</summary>
    public const int RuntimeSettingsVersion = 1;

    private const string LegacyThumbnailKey = "ThumbnmailFileName";
    private const string ThumbnailKey = "ThumbnailFileName";

    private const int ItemTypeOffset = 1;

    /// <summary>
    /// アイテムの JSON 配列に対して、指定したターゲットバージョンまでのマイグレーションを適用します。
    /// </summary>
    /// <param name="items">マイグレーション対象のアイテム JSON 配列。</param>
    /// <param name="targetVersion">適用するスキーマバージョン。</param>
    /// <param name="dataRootDirectory">データのルートディレクトリ（相対パス化などの処理で使用）。省略可能。</param>
    /// <returns>いずれかのマイグレーションで内容が変更された場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public static bool ApplyItemMigration(JsonArray items, int targetVersion, string? dataRootDirectory = null)
    {
        return targetVersion switch
        {
            1 => MigrateV1RenameThumbnailKey(items),
            2 => MigrateV2ItemTypeOffset(items),
            3 => MigrateV3ItemRefactor(items, dataRootDirectory),
            4 => MigrateV4ConvertToRelativePath(items, dataRootDirectory),
            _ => false
        };
    }

    /// <summary>
    /// 共通素体の JSON 配列に対して、指定したターゲットバージョンまでのマイグレーションを適用します。
    /// </summary>
    /// <param name="items">マイグレーション対象の共通素体 JSON 配列。</param>
    /// <param name="targetVersion">適用するスキーマバージョン。</param>
    /// <returns>内容が変更された場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public static bool ApplyCommonAvatarMigration(JsonArray items, int targetVersion)
    {
        return targetVersion switch
        {
            1 => MigrateV1AvatarReferences(items, "Avatars"),
            _ => false
        };
    }

    /// <summary>
    /// 一括インポートプリセットの JSON 配列に対して、指定したターゲットバージョンまでのマイグレーションを適用します。
    /// </summary>
    /// <param name="items">マイグレーション対象のプリセット JSON 配列。</param>
    /// <param name="targetVersion">適用するスキーマバージョン。</param>
    /// <returns>内容が変更された場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public static bool ApplyBulkImportPresetMigration(JsonArray items, int targetVersion)
    {
        return targetVersion switch
        {
            1 => MigrateV1BulkImportItemIds(items),
            _ => false
        };
    }

    /// <summary>
    /// ランタイム設定の JSON オブジェクトに対して、指定したターゲットバージョンまでのマイグレーションを適用します。
    /// </summary>
    /// <param name="settings">マイグレーション対象の設定 JSON オブジェクト。</param>
    /// <param name="targetVersion">適用するスキーマバージョン。</param>
    /// <returns>内容が変更された場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public static bool ApplyRuntimeSettingsMigration(JsonObject settings, int targetVersion)
    {
        _ = settings;
        return targetVersion switch
        {
            1 => false, // Obsolete fields remain for UI-side UserPreferences migration
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

    private static bool MigrateV3ItemRefactor(JsonArray items, string? dataRootDirectory)
    {
        var changed = false;

        foreach (var itemNode in items)
        {
            if (itemNode is not JsonObject item) continue;

            if (MigrateItemPath(item, dataRootDirectory)) changed = true;
            if (MigrateAvatarReferences(item, "SupportedAvatars")) changed = true;
            if (MigrateAvatarReferences(item, "ImplementedAvatars")) changed = true;
            if (MigrateCategory(item)) changed = true;
        }

        return changed;
    }

    private static bool MigrateV4ConvertToRelativePath(JsonArray items, string? dataRootDirectory)
    {
        if (string.IsNullOrEmpty(dataRootDirectory)) return false;

        var changed = false;

        foreach (var itemNode in items)
        {
            if (itemNode is not JsonObject item) continue;

            // Convert to relative => <root> prefix
            if (!item.TryGetPropertyValue("ItemPath", out var pathNode) ||
                pathNode is not JsonValue pathValue ||
                !pathValue.TryGetValue(out string? path) ||
                !path.StartsWith(dataRootDirectory))
            {
                continue;
            }

            var newPath = ItemUtils.GetRelativePath(path, dataRootDirectory);
            item["ItemPath"] = JsonValue.Create(newPath);
            changed = true;
        }

        return changed;
    }

    private static bool MigrateItemPath(JsonObject item, string? dataRootDirectory)
    {
        if (string.IsNullOrEmpty(dataRootDirectory)) return false;

        if (!item.TryGetPropertyValue("ItemPath", out var pathNode) ||
            pathNode is not JsonValue pathValue ||
            !pathValue.TryGetValue(out string? path) ||
            !path.StartsWith("<sys>"))
        {
            return false;
        }

        var newPath = Path.Join(dataRootDirectory, path.Replace("<sys>", string.Empty));
        item["ItemPath"] = JsonValue.Create(newPath);
        return true;
    }

    private static bool MigrateAvatarReferences(JsonObject item, string propertyName)
    {
        if (!item.TryGetPropertyValue(propertyName, out var arrayNode) ||
            arrayNode is not JsonArray avatars)
        {
            return false;
        }

        var changed = false;
        var newAvatars = new JsonArray();

        foreach (var avatarNode in avatars)
        {
            if (avatarNode is not JsonValue avatarValue ||
                !avatarValue.TryGetValue(out string? avatar))
            {
                newAvatars.Add(avatarNode?.DeepClone());
                continue;
            }

            string newValue;
            if (avatar.StartsWith("<sys:temp>"))
            {
                newValue = avatar.Replace("<sys:temp>", "tempavatar:");
                changed = true;
            }
            else if (avatar.StartsWith("<sys:commonavatar>"))
            {
                newValue = avatar.Replace("<sys:commonavatar>", "commonavatar:");
                changed = true;
            }
            else if (!avatar.StartsWith("item:") && !avatar.StartsWith("tempavatar:") && !avatar.StartsWith("commonavatar:"))
            {
                newValue = "item:" + avatar;
                changed = true;
            }
            else
            {
                newValue = avatar;
            }

            newAvatars.Add(JsonValue.Create(newValue));
        }

        if (changed) item[propertyName] = newAvatars;
        return changed;
    }

    private static bool MigrateV1AvatarReferences(JsonArray items, string propertyName)
    {
        var changed = false;

        foreach (var itemNode in items)
        {
            if (itemNode is not JsonObject item) continue;
            if (MigrateAvatarReferences(item, propertyName)) changed = true;
        }

        return changed;
    }

    private static bool MigrateCategory(JsonObject item)
    {
        var typeNode = item["Type"];
        var customCategoryNode = item["CustomCategory"];

        int type = 0;
        string customCategory = string.Empty;

        if (typeNode is JsonValue typeValue && typeValue.TryGetValue(out int t))
            type = t;
        if (customCategoryNode is JsonValue categoryValue && categoryValue.TryGetValue(out string? c) && c != null)
            customCategory = c;

        var categoryObject = new JsonObject
        {
            ["Type"] = type,
            ["CustomCategory"] = customCategory
        };

        item["Category"] = categoryObject;
        item.Remove("Type");
        item.Remove("CustomCategory");
        return true;
    }

    private static bool MigrateV1BulkImportItemIds(JsonArray items)
    {
        var changed = false;

        foreach (var presetNode in items)
        {
            if (presetNode is not JsonObject preset) continue;
            if (!preset.TryGetPropertyValue("Items", out var itemsNode) ||
                itemsNode is not JsonArray presetItems)
            {
                continue;
            }

            foreach (var presetItemNode in presetItems)
            {
                if (presetItemNode is not JsonObject presetItem)
                {
                    continue;
                }

                if (presetItem.TryGetPropertyValue("ItemId", out var itemIdNode) &&
                    itemIdNode is JsonValue itemIdValue &&
                    itemIdValue.TryGetValue(out string? itemId) &&
                    !itemId.StartsWith("item:"))
                {
                    presetItem["ItemId"] = JsonValue.Create("item:" + itemId);
                    changed = true;
                }
            }
        }

        return changed;
    }
}
