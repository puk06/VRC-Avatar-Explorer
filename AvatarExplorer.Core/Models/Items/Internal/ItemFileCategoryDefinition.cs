namespace AvatarExplorer.Core.Models.Items.Internal;

internal class ItemFileCategoryDefinition
{
    internal ItemFileCategoryType FileCategory { get; set; } = ItemFileCategoryType.None;
    internal string[]? ExtensionFilters { get; set; } = null;
    internal string[]? FilenameFilters { get; set; } = null;
    internal FileCategoryItem Item { get; set; } = new();
}
