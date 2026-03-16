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
    internal static ImmutableArray<Item> ExecuteSearch(IEnumerable<Item> items, IEnumerable<CommonAvatar> commonAvatars, Dictionary<string, string> searchIndexDictionary, RuntimeSettings runtimeSettings, SearchFilter searchFilter)
    {
        Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(items.Where(i => i.Type == ItemType.Avatar));

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
            .OrderByDescending(i => !searchIndexDictionary.TryGetValue(i.Id, out string? value) ? 0 : SearchUtils.GetScore(value, searchFilter.SearchWords))
            .ToImmutableArray();
    }
    private static bool Matches(Item item, SearchFilter searchFilter, string searchIndex, Dictionary<string, string> avatarTitleMaps, IEnumerable<CommonAvatar> commonAvatars, string parentFolder)
    {
        bool matchTitle = searchFilter.Titles.Count == 0 || SearchUtils.MatchesFilter(
            [item.Title], searchFilter.Titles,
            searchFilter.IsOrSearch,
            (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchAuthor = searchFilter.Authors.Count == 0 || SearchUtils.MatchesFilter(
            [item.Author], searchFilter.Authors,
            searchFilter.IsOrSearch,
            (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchBooth = searchFilter.BoothIds.Count == 0 || SearchUtils.MatchesFilter(
            [item.BoothId.ToString()], searchFilter.BoothIds,
            searchFilter.IsOrSearch,
            (target, filter) => target == filter
        );

        bool matchAvatar = searchFilter.SupportedAvatars.Count == 0 || SearchUtils.MatchesFilter(
            AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, commonAvatars, includeCommonAvatarToSupported: true)
                .Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i)),
            searchFilter.SupportedAvatars,
            searchFilter.IsOrSearch,
            (target, filter) => !string.IsNullOrEmpty(target) && target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchCategory = searchFilter.Categories.Count == 0 || SearchUtils.MatchesFilter(
            [item.Type == ItemType.Custom ? item.CustomCategory : item.Type.GetLocalizationKey()], searchFilter.Categories,
            searchFilter.IsOrSearch,
            (target, filter) => target != null && target.Contains(filter!)
        );

        bool matchMemo = searchFilter.ItemMemos.Count == 0 || SearchUtils.MatchesFilter(
            [item.ItemMemo], searchFilter.ItemMemos,
            searchFilter.IsOrSearch,
            (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchPath = searchFilter.FolderNames.Count == 0 || SearchUtils.MatchesFilter(
            [Path.GetFileName(item.ItemPath)], searchFilter.FolderNames,
            searchFilter.IsOrSearch,
            (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchFile;
        if (searchFilter.FileNames.Count == 0)
        {
            matchFile = true;
        }
        else
        {
            string itemPath = ItemUtils.GetItemPath(parentFolder, item.ItemPath);

            List<string> files = new();
            if (!string.IsNullOrEmpty(itemPath) && Directory.Exists(itemPath)) files.AddRange(FileSystemService.EnumerateFiles(itemPath));

            matchFile = SearchUtils.MatchesFilter(
                files, searchFilter.FileNames,
                searchFilter.IsOrSearch,
                (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
            );
        }

        IEnumerable<string> implementedAvatarTitles = item.ImplementedAvatarsView.Select(i => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, i));

        bool matchImplemented = searchFilter.ImplementedAvatars.Count == 0 || SearchUtils.MatchesFilter(
            implementedAvatarTitles, searchFilter.ImplementedAvatars,
            searchFilter.IsOrSearch,
            (target, filter) => !string.IsNullOrEmpty(target) && target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchNotImplemented = searchFilter.NotImplementedAvatars.Count == 0
            || (searchFilter.NotImplementedAvatars.Count > 0 && searchFilter.IsOrSearch
                ? searchFilter.NotImplementedAvatars.Any(filter => !implementedAvatarTitles.Any(name => name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
                : searchFilter.NotImplementedAvatars.All(filter => !implementedAvatarTitles.Any(name => name.Contains(filter, StringComparison.CurrentCultureIgnoreCase))));

        bool matchTag = searchFilter.Tags.Count == 0 || SearchUtils.MatchesFilter(
            item.TagsView, searchFilter.Tags,
            searchFilter.IsOrSearch,
            (target, filter) => target.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        );

        bool matchCommon;
        if (searchFilter.CommonAvatars.Count == 0)
        {
            matchCommon = true;
        }
        else
        {
            List<CommonAvatar?> filterCommonAvatars = searchFilter.CommonAvatars
                .Select(name => CommonAvatarService.GetCommonAvatarFromName(commonAvatars, name))
                .ToList();

            matchCommon = searchFilter.IsOrSearch
                ? item.SupportedAvatarsView.Any(avatar => filterCommonAvatars.Any(ca => ca != null && ca.AvatarsView.Contains(avatar)))
                : filterCommonAvatars.All(ca => ca != null && item.SupportedAvatarsView.Any(avatar => ca.AvatarsView.Contains(avatar)));
        }

        bool matchBroken = !searchFilter.BrokenItems || (searchFilter.BrokenItems && !(item.SupportedAvatarsView.Contains(item.ItemPath) || item.ImplementedAvatarsView.Contains(item.ItemPath)));

        bool matchWord = searchFilter.SearchWords.Count == 0
            || (searchFilter.IsOrSearch
                ? searchFilter.SearchWords.Any(word => SearchUtils.GetWordSearchResult(searchIndex, word))
                : searchFilter.SearchWords.All(word => SearchUtils.GetWordSearchResult(searchIndex, word)));

        return matchTitle
            && matchAuthor
            && matchBooth
            && matchAvatar
            && matchCategory
            && matchMemo
            && matchPath
            && matchFile
            && matchImplemented
            && matchNotImplemented
            && matchTag
            && matchCommon
            && matchBroken
            && matchWord;
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
