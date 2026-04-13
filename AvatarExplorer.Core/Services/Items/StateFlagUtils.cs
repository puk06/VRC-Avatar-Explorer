using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

public static class StateFlagUtils
{
    public static readonly ItemTagStates ItemFlags = ItemTagStates.SearchItem | ItemTagStates.RootAvatar | ItemTagStates.RootSelectedItem | ItemTagStates.RootItem;
    public static readonly ItemTagStates DraggableFlags = ItemTagStates.SearchItem | ItemTagStates.RootAvatar | ItemTagStates.RootSelectedItem | ItemTagStates.ItemFileCategoryOpen | ItemTagStates.RootItem;
    public static readonly ItemTagStates CategoryFlags = ItemTagStates.RootCategory | ItemTagStates.RootSelectedCategory | ItemTagStates.ItemFileCategory;

    public static bool IsItemState(ItemTagStates itemTagState) => itemTagState != ItemTagStates.None && ItemFlags.HasFlag(itemTagState);
    public static bool IsCategoryState(ItemTagStates itemTagState) => itemTagState != ItemTagStates.None && CategoryFlags.HasFlag(itemTagState);
    public static bool IsDraggableState(ItemTagStates itemTagState) => itemTagState != ItemTagStates.None && DraggableFlags.HasFlag(itemTagState);
}
