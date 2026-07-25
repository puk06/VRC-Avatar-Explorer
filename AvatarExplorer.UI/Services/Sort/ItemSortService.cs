using System;
using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Utils;

namespace AvatarExplorer.UI.Services.Sort;

public static class ItemSortService
{
    public static IEnumerable<Item> Sort(IEnumerable<Item> items, ItemSortOrder order, SortDirection direction, bool removeBrackets)
    {
        var ordered = order switch
        {
            ItemSortOrder.Title => items.OrderBy(i => removeBrackets ? TextBracketsUtils.RemoveBrackets(i.Title) : i.Title, StringComparer.OrdinalIgnoreCase),
            ItemSortOrder.Author => items.OrderBy(i => i.Author, StringComparer.OrdinalIgnoreCase),
            ItemSortOrder.CreatedDate => items.OrderBy(i => i.CreatedDate),
            ItemSortOrder.UpdatedDate => items.OrderBy(i => i.UpdatedDate),
            _ => items.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
        };

        return direction == SortDirection.Descending ? ordered.Reverse() : ordered;
    }
}
