using System.Text.Json.Nodes;

namespace AvatarExplorer.Core.Services.IO;

public static class DatabaseMigrations
{
    public const int ItemVersion = 3;
    public const int CommonAvatarVersion = 1;
    public const int BulkImportPresetVersion = 1;
    public const int RuntimeSettingsVersion = 1;

    private const string LegacyThumbnailKey = "ThumbnmailFileName";
    private const string ThumbnailKey = "ThumbnailFileName";

    private const int ItemTypeOffset = 1;

    public static bool ApplyItemMigration(JsonNode root, int targetVersion, string? dataRootDirectory = null)
    {
        if (root is not JsonArray items) return false;

        return targetVersion switch
        {
            1 => MigrateV1RenameThumbnailKey(items),
            2 => MigrateV2ItemTypeOffset(items),
            3 => MigrateV3ItemRefactor(items, dataRootDirectory),
            _ => false
        };
    }

    public static bool ApplyCommonAvatarMigration(JsonNode root, int targetVersion)
    {
        if (root is not JsonArray items) return false;

        return targetVersion switch
        {
            1 => MigrateV1AvatarReferences(items, "Avatars"),
            _ => false
        };
    }

    public static bool ApplyBulkImportPresetMigration(JsonNode root, int targetVersion)
    {
        if (root is not JsonArray items) return false;

        return targetVersion switch
        {
            1 => MigrateV1BulkImportItemIds(items),
            _ => false
        };
    }

    public static bool ApplyRuntimeSettingsMigration(JsonNode root, int targetVersion)
    {
        if (root is not JsonObject settings) return false;

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

    private static bool MigrateItemPath(JsonObject item, string? dataRootDirectory)
    {
        if (string.IsNullOrEmpty(dataRootDirectory)) return false;

        if (!item.TryGetPropertyValue("ItemPath", out var pathNode) ||
            pathNode is not JsonValue pathValue ||
            !pathValue.TryGetValue(out string? path) ||
            !path.StartsWith("<sys>"))
            return false;

        var newPath = Path.Join(dataRootDirectory, path.Replace("<sys>", string.Empty));
        item["ItemPath"] = JsonValue.Create(newPath);
        return true;
    }

    private static bool MigrateAvatarReferences(JsonObject item, string propertyName)
    {
        if (!item.TryGetPropertyValue(propertyName, out var arrayNode) ||
            arrayNode is not JsonArray avatars)
            return false;

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
                continue;

            foreach (var presetItemNode in presetItems)
            {
                if (presetItemNode is not JsonObject presetItem) continue;

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
