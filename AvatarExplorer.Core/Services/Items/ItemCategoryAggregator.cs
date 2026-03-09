using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemCategoryAggregator
{
    internal static IReadOnlyList<ItemCountInfo> Aggregate(IEnumerable<Item> items, bool includeEmptyCategory = false)
    {
        List<ItemCountInfo> categories = new();

        Dictionary<ItemType, int> itemsByType = items
            .GroupBy(i => i.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        Dictionary<string, int> itemsByCustomCategory = items
            .Where(i => !string.IsNullOrEmpty(i.CustomCategory))
            .GroupBy(i => i.CustomCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        categories.AddRange(
            Enum.GetValues<ItemType>()
                .Where(i => !CategoryUtils.InvalidItemTypes.Contains(i) && i != ItemType.Custom)
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

        return categories;
    }
}
