using System.Text.Json.Nodes;

namespace AvatarExplorer.Core.Services.IO;

public static class DatabaseMigrations
{
    public const int ItemVersion = 3;
    public const int CommonAvatarVersion = 1;
    public const int BulkImportPresetVersion = 1;
    public const int RuntimeSettingsVersion = 1;
    public const int UserPreferencesVersion = 1;

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

    public static bool ApplyRuntimeSettingsMigration(JsonNode root, int targetVersion, string? userPreferencesFilePath = null)
    {
        if (root is not JsonObject settings) return false;

        return targetVersion switch
        {
            1 => MigrateRuntimeSettingsV1ToPreferences(settings, userPreferencesFilePath),
            _ => false
        };
    }

    public static bool ApplyUserPreferencesMigration(JsonNode root, int targetVersion, string? runtimeSettingsFilePath = null)
    {
        if (root is not JsonObject preferences) return false;

        return targetVersion switch
        {
            1 => MigrateUserPreferencesV1FromRuntime(preferences, runtimeSettingsFilePath),
            _ => false
        };
    }

    private static bool MigrateRuntimeSettingsV1ToPreferences(JsonObject settings, string? userPreferencesFilePath)
    {
        if (string.IsNullOrEmpty(userPreferencesFilePath)) return false;

        var hasItemSortOrder = settings.ContainsKey("ItemSortOrder");
        var hasRemoveBrackets = settings.ContainsKey("RemoveBrackets");
        if (!hasItemSortOrder && !hasRemoveBrackets) return false;

        var sortOrder = 3; // UpdatedDate
        var removeBrackets = false;

        if (hasItemSortOrder)
        {
            var sortNode = settings["ItemSortOrder"];
            if (sortNode is JsonValue sortValue && sortValue.TryGetValue(out int s))
                sortOrder = s;
        }

        if (hasRemoveBrackets)
        {
            var bracketNode = settings["RemoveBrackets"];
            if (bracketNode is JsonValue bracketValue && bracketValue.TryGetValue(out bool b))
                removeBrackets = b;
        }

        // Apply to preferences
        var preferences = File.Exists(userPreferencesFilePath)
            ? (JsonNode.Parse(File.ReadAllText(userPreferencesFilePath)) as JsonObject) ?? []
            : [];

        preferences["SortOrder"] = sortOrder;
        preferences["RemoveBrackets"] = removeBrackets;

        // Backup and save preferences
        var prefsAppliedVersion = DatabaseMigrationService.ReadAppliedMigrationVersion(userPreferencesFilePath);
        if (File.Exists(userPreferencesFilePath))
        {
            var prefsBackup = BuildMigrationBackupPath(userPreferencesFilePath, prefsAppliedVersion, UserPreferencesVersion);
            File.Copy(userPreferencesFilePath, prefsBackup, overwrite: true);
        }
        File.WriteAllText(userPreferencesFilePath, preferences.ToJsonString(JsonManager.JsonSerializerOptions));
        DatabaseMigrationService.WriteAppliedMigrationVersion(userPreferencesFilePath, UserPreferencesVersion);

        // Remove obsolete fields from runtime settings
        if (hasItemSortOrder) settings.Remove("ItemSortOrder");
        if (hasRemoveBrackets) settings.Remove("RemoveBrackets");

        return true;
    }

    private static bool MigrateUserPreferencesV1FromRuntime(JsonObject preferences, string? runtimeSettingsFilePath)
    {
        if (string.IsNullOrEmpty(runtimeSettingsFilePath) || !File.Exists(runtimeSettingsFilePath))
            return false;

        var runtimeJson = File.ReadAllText(runtimeSettingsFilePath);
        var runtime = JsonNode.Parse(runtimeJson) as JsonObject;
        if (runtime is null) return false;

        var hasItemSortOrder = runtime.ContainsKey("ItemSortOrder");
        var hasRemoveBrackets = runtime.ContainsKey("RemoveBrackets");
        if (!hasItemSortOrder && !hasRemoveBrackets) return false;

        if (hasItemSortOrder)
        {
            var sortNode = runtime["ItemSortOrder"];
            if (sortNode is JsonValue sortValue && sortValue.TryGetValue(out int s))
                preferences["SortOrder"] = s;
        }

        if (hasRemoveBrackets)
        {
            var bracketNode = runtime["RemoveBrackets"];
            if (bracketNode is JsonValue bracketValue && bracketValue.TryGetValue(out bool b))
                preferences["RemoveBrackets"] = b;
        }

        return true;
    }

    private static string BuildMigrationBackupPath(string filePath, int fromVersion, int toVersion)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        return Path.Combine(directory, $"{name}.migration-v{fromVersion}-to-v{toVersion}.bak{extension}");
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
