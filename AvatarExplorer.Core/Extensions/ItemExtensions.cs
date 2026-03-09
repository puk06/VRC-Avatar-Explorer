using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Extensions;

public static class ItemExtensions
{
    internal static IEnumerable<Item> GetSortedItems(this IEnumerable<Item> items, RuntimeSettings runtimeSettings)
    {
        return runtimeSettings.ItemSortOrder switch
        {
            ItemSortOrder.Title => items.OrderBy(item => runtimeSettings.RemoveBrackets ? ItemUtils.RemoveBrackets(item.Title) : item.Title),
            ItemSortOrder.Author => items.OrderBy(item => item.Author),
            ItemSortOrder.Created => items.OrderByDescending(item => item.CreatedDate),
            ItemSortOrder.Updated => items.OrderByDescending(item => item.UpdatedDate),
            _ => items.OrderBy(item => item.Title)
        };
    }

    internal static IEnumerable<ItemCountInfo> GetSortedItemsFromCountInfo(this IEnumerable<ItemCountInfo> itemCountInfos, RuntimeSettings runtimeSettings)
    {
        if (itemCountInfos.Any(i => i.Item is not Item)) return itemCountInfos;

        return runtimeSettings.ItemSortOrder switch
        {
            ItemSortOrder.Title => itemCountInfos.OrderBy(i => runtimeSettings.RemoveBrackets ? ItemUtils.RemoveBrackets(((Item)i.Item).Title) : ((Item)i.Item).Title),
            ItemSortOrder.Author => itemCountInfos.OrderBy(i => ((Item)i.Item).Author),
            ItemSortOrder.Created => itemCountInfos.OrderByDescending(i => ((Item)i.Item).CreatedDate),
            ItemSortOrder.Updated => itemCountInfos.OrderByDescending(i => ((Item)i.Item).UpdatedDate),
            _ => itemCountInfos.OrderBy(i => ((Item)i.Item).Title)
        };
    }

    internal static bool IsCategoryMatch(this Item item, string category)
    {
        if (item.Type == ItemType.Custom) return item.CustomCategory == category;
        else return item.Type.GetLocalizationKey() == category;
    }
}
