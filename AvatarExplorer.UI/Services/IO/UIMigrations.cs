using System.IO;
using System.Text.Json.Nodes;

namespace AvatarExplorer.UI.Services.IO;

public static class UIMigrations
{
    public const int UserPreferencesVersion = 1;

    public static bool ApplyUserPreferencesMigration(JsonNode root, int targetVersion, string? runtimeSettingsFilePath = null)
    {
        if (root is not JsonObject preferences) return false;

        return targetVersion switch
        {
            1 => MigrateUserPreferencesV1FromRuntime(preferences, runtimeSettingsFilePath),
            _ => false
        };
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
}
