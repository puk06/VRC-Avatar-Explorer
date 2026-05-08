using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemCategoryAggregator
{
    internal static ImmutableArray<ItemCountInfo> Aggregate(IEnumerable<Item> items, bool includeEmptyCategory = false, bool includeAllCategory = false)
    {
        var categories = ImmutableArray.CreateBuilder<ItemCountInfo>();

        Dictionary<ItemType, int> itemsByType = items
            .GroupBy(i => i.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        Dictionary<string, int> itemsByCustomCategory = items
            .Where(i => !string.IsNullOrEmpty(i.CustomCategory))
            .GroupBy(i => i.CustomCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        if (includeAllCategory)
        {
            categories.Add(new ItemCountInfo(new ItemCategory(ItemType.All), items.Count()));
        }

        categories.AddRange(
            Enum.GetValues<ItemType>()
                .Where(i => i.IsSelectable())
                .Where(i => includeEmptyCategory || itemsByType.ContainsKey(i))
                .Select(i => new ItemCountInfo(new ItemCategory(i), itemsByType.TryGetValue(i, out int count) ? count : 0))
        );

        categories.AddRange(
            items
                .Select(i => i.CustomCategory)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct()
                .Where(itemsByCustomCategory.ContainsKey)
                .Select(i => new ItemCountInfo(new ItemCategory(i), itemsByCustomCategory[i]))
        );

        return categories.ToImmutable();
    }
}
