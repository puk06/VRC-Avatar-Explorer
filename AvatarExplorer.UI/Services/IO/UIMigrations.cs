using System.IO;
using System.Text.Json.Nodes;

namespace AvatarExplorer.UI.Services.IO;

public static class UIMigrations
{
    public const int UserPreferencesVersion = 1;

    public static bool ApplyUserPreferencesMigration(JsonObject preferences, int targetVersion, string? runtimeSettingsFilePath = null)
    {
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
        if (JsonNode.Parse(runtimeJson) is not JsonObject runtime) return false;

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
