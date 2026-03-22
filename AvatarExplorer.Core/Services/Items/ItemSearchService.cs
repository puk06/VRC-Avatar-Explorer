using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemSearchService
{
    internal static ImmutableArray<Item> ExecuteSearch(IEnumerable<Item> items, IEnumerable<CommonAvatar> commonAvatars, IEnumerable<TempAvatar> tempAvatars, Dictionary<string, string> searchIndexDictionary, RuntimeSettings runtimeSettings, SearchFilter searchFilter)
    {
        Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(items.Where(i => i.Type == ItemType.Avatar), tempAvatars);

        List<Item> matchedItems = new();
        foreach (Item item in items)
        {
            string searchIndex = searchIndexDictionary.TryGetValue(item.Id, out string? value) ? value : string.Empty;
            if (Matches(item, searchFilter, searchIndex, avatarTitleMaps, commonAvatars, runtimeSettings.DataRootDirectory))
            {
                matchedItems.Add(item);
            }
        }

        return matchedItems
            .OrderByDescending(i => !searchIndexDictionary.TryGetValue(i.Id, out string? value) ? 0 : SearchUtils.GetScore(value, searchFilter.SearchTokens.Where(t => t.Type == SearchTokenType.FreeWord).Select(t => t.Value)))
            .ToImmutableArray();
    }
    private static bool Matches(Item item, SearchFilter searchFilter, string searchIndex, Dictionary<string, string> avatarTitleMaps, IEnumerable<CommonAvatar> commonAvatars, string parentFolder)
    {
        string[] getTargets(SearchTokenType type)
        {
            return type switch
            {
                SearchTokenType.Title => [item.Title],
                SearchTokenType.Author => [item.Author],
                SearchTokenType.BoothId => [item.BoothId.ToString()],
                SearchTokenType.SupportedAvatar => item.SupportedAvatarsView.Length > 0
                    ? AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, commonAvatars, includeCommonAvatarToSupported: true)
                        .Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i))
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToArray()
                    : avatarTitleMaps.Values.ToArray(),
                SearchTokenType.Category => [item.Type == ItemType.Custom ? item.CustomCategory : (item.Type.GetLocalizationKey() ?? string.Empty)],
                SearchTokenType.ItemMemo => [item.ItemMemo],
                SearchTokenType.FolderName => [Path.GetFileName(item.ItemPath)],
                SearchTokenType.FileName => !string.IsNullOrEmpty(ItemUtils.GetItemPath(parentFolder, item.ItemPath)) && Directory.Exists(ItemUtils.GetItemPath(parentFolder, item.ItemPath)) && searchFilter.SearchTokens.Any(i => i.Type == SearchTokenType.FileName)
                    ? FileSystemService.EnumerateFiles(ItemUtils.GetItemPath(parentFolder, item.ItemPath)).ToArray()
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
            return !searchFilter.SearchTokens
                .GroupBy(i => i.Type)
                .Any(i => !i.Any(token => getTargets(i.Key).Any(target => target.Contains(token.Value, StringComparison.CurrentCultureIgnoreCase))));
        }
        else
        {
            foreach (SearchToken token in searchFilter.SearchTokens)
            {
                bool isNegation = token.Value.StartsWith('~');
                string filterValue = isNegation ? token.Value[1..] : token.Value;

                string[] targets = getTargets(token.Type);

                if (searchFilter.IsOrCondition)
                {
                    if (isNegation)
                        if (targets.All(target => !target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase))) return true;
                    else
                        if (targets.Any(target => target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase))) return true;
                }
                else
                {
                    if (isNegation)
                        if (!targets.All(target => !target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase))) return false;
                    else
                        if (!targets.Any(target => target.Contains(filterValue, StringComparison.CurrentCultureIgnoreCase))) return false;
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
