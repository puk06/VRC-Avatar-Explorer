using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemSearchService
{
    internal static ImmutableArray<Item> ExecuteSearch(SearchContext searchContext, SearchFilter searchFilter)
    {
        var matchedItems = new List<Item>();

        foreach (var item in searchContext.Items)
        {
            if (!searchContext.ItemSearchIndices.TryGetValue(item.Id, out var searchIndex))
                continue;

            if (Matches(searchIndex, searchFilter))
            {
                matchedItems.Add(item);
            }
        }

        return matchedItems
            .OrderByDescending(i =>
            {
                if (!searchContext.ItemSearchIndices.TryGetValue(i.Id, out var index))
                    return 0;
                return SearchUtils.GetScore(
                    index.FreeWord,
                    searchFilter.SearchTokens.Where(t => t.Type == SearchTokenType.FreeWord).Select(t => t.Value));
            })
            .ToImmutableArray();
    }

    private static bool Matches(ItemSearchIndex searchIndex, SearchFilter searchFilter)
    {
        if (searchFilter.IsCategoryOrCondition)
        {
            var tokenGroups = searchFilter.SearchTokens.GroupBy(t => t.Type);

            foreach (var group in tokenGroups)
            {
                bool hasMatchInGroup = group.Any(token => searchIndex.IsMatch(token));
                if (!hasMatchInGroup)
                    return false;
            }

            return true;
        }
        else
        {
            foreach (var token in searchFilter.SearchTokens)
            {
                bool isMatch = searchIndex.IsMatch(token);

                if (searchFilter.IsOrCondition)
                {
                    if (isMatch) return true;
                }
                else
                {
                    if (!isMatch) return false;
                }
            }

            return !searchFilter.IsOrCondition;
        }
    }
}
