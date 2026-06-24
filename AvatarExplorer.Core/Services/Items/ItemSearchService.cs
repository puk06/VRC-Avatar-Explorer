using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemSearchService
{
    internal static ImmutableArray<Item> ExecuteSearch(SearchContext searchContext, SearchFilter searchFilter)
    {
        Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(searchContext.Items.Where(i => i.Type == ItemType.Avatar), searchContext.TempAvatars);

        List<Item> matchedItems = new();
        foreach (Item item in searchContext.Items)
        {
            string searchIndex = searchContext.SearchIndexDictionary.TryGetValue(item.Id, out string? value) ? value : string.Empty;
            if (Matches(item, searchFilter, searchIndex, avatarTitleMaps, searchContext.CommonAvatars, searchContext.RuntimeSettings.DataRootDirectory))
            {
                matchedItems.Add(item);
            }
        }

        return matchedItems
            .OrderByDescending(i => !searchContext.SearchIndexDictionary.TryGetValue(i.Id, out string? value) ? 0 : SearchUtils.GetScore(value, searchFilter.SearchTokens.Where(t => t.Type == SearchTokenType.FreeWord).Select(t => t.Value)))
            .ToImmutableArray();
    }
    private static bool Matches(Item item, SearchFilter searchFilter, string searchIndex, Dictionary<string, string> avatarTitleMaps, IEnumerable<CommonAvatar> commonAvatars, string parentFolder)
    {
        string[] getTargets(SearchTokenType type)
        {
            string[] getSupportedAvatarTargets()
            {
                if (item.SupportedAvatarsView.Length > 0)
                {
                    return AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, commonAvatars, includeCommonAvatarToSupported: true)
                        .Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i))
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToArray();
                }

                return searchFilter.TreatEmptySupportedAvatarAsNone ? Array.Empty<string>() : avatarTitleMaps.Values.ToArray();
            }

            return type switch
            {
                SearchTokenType.Title => [item.Title],
                SearchTokenType.Author => [item.Author],
                SearchTokenType.BoothId => [item.BoothId.ToString()],
                SearchTokenType.SupportedAvatar => getSupportedAvatarTargets(),
                SearchTokenType.Category => [item.Type == ItemType.Custom ? item.CustomCategory : (item.Type.GetLocalizationKey() ?? string.Empty)],
                SearchTokenType.ItemMemo => [item.ItemMemo],
                SearchTokenType.FolderName => item.GetFolderPaths(parentFolder).Select(Path.GetFileName).Where(i => !string.IsNullOrEmpty(i)).Cast<string>().ToArray(),
                SearchTokenType.FileName => searchFilter.SearchTokens.Any(i => i.Type == SearchTokenType.FileName)
                    ? item.EnumerateFiles(parentFolder).Select(Path.GetFileName).Where(i => !string.IsNullOrEmpty(i)).Cast<string>().ToArray()
                    : Array.Empty<string>(),
                SearchTokenType.ImplementedAvatar => item.ImplementedAvatarsView.Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i)).Where(name => !string.IsNullOrEmpty(name)).ToArray(),
                SearchTokenType.NotImplementedAvatar => avatarTitleMaps.Keys.Except(item.ImplementedAvatarsView).Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i)).Where(name => !string.IsNullOrEmpty(name)).ToArray(),
                SearchTokenType.Tag => item.TagsView.ToArray(),
                SearchTokenType.CommonAvatar => commonAvatars.Where(ca => ca.AvatarsView.Any(a => item.SupportedAvatarsView.Contains(a))).Select(ca => ca.GroupName).ToArray(),
                SearchTokenType.FreeWord => [searchIndex],
                _ => Array.Empty<string>()
            };
        }

        if (searchFilter.IsCategoryOrCondition)
        {
            var tokenGroups = searchFilter.SearchTokens.GroupBy(t => t.Type);

            foreach (var group in tokenGroups)
            {
                string[] targets = getTargets(group.Key);

                bool hasMatchInThisGroup = group.Any(token =>
                {
                    if (token.IsNegation)
                        return targets.All(target => !target.Contains(token.Value, StringComparison.CurrentCultureIgnoreCase));
                    else
                        return targets.Any(target => target.Contains(token.Value, StringComparison.CurrentCultureIgnoreCase));
                });

                if (!hasMatchInThisGroup)
                    return false;
            }

            return true;
        }
        else
        {
            foreach (SearchToken token in searchFilter.SearchTokens)
            {
                bool isNegation = token.IsNegation;
                string filterValue = token.Value;

                string[] targets = getTargets(token.Type);

                if (searchFilter.IsOrCondition)
                {
                    if (isNegation)
                    {
                        if (targets.All(target => !target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)))
                            return true;
                    }
                    else
                    {
                        if (targets.Any(target => target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)))
                            return true;
                    }
                }
                else
                {
                    if (isNegation)
                    {
                        if (!targets.All(target => !target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)))
                            return false;
                    }
                    else
                    {
                        if (!targets.Any(target => target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase)))
                            return false;
                    }
                }
            }

            return !searchFilter.IsOrCondition;
        }
    }

    internal static string BuildItemSearchIndex(Item item, Dictionary<string, string> avatarTitleMaps, IEnumerable<CommonAvatar> commonAvatars)
    {
        IEnumerable<string> avatars = AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, commonAvatars, includeCommonAvatarToSupported: true)
            .Concat(item.ImplementedAvatarsView)
            .Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i))
            .Where(name => !string.IsNullOrEmpty(name));

        return string.Join("\n",
            item.Title,
            item.Author,
            item.ItemMemo,
            item.BoothId.ToString(),
            string.Join(" ", item.TagsView),
            string.Join(" ", avatars)
        ).ToLowerInvariant();
    }
}
