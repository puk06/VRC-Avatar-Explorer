using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.Extensions;

internal static class SearchFilterExtensions
{
    internal static string ToPathString(this SearchFilter searchFilter)
    {
        List<string> searchFilterStrings = new();

        string localize(string key, IEnumerable<string> values) => Localizer.Instance.Get(key, toSeparatedString(values));
        void addKey(string key, IEnumerable<string> values) => searchFilterStrings.Add(localize(key, values));
        string toSeparatedString(IEnumerable<string> values, string separateString = ", ") => string.Join(separateString, values);

        if (searchFilter.IsOrSearch) searchFilterStrings.Add(Localizer.Instance[LocalizationKey.SearchFilter.IsOrSearch]);
        if (searchFilter.Titles.Count != 0) addKey(LocalizationKey.SearchFilter.Title, searchFilter.Titles);
        if (searchFilter.Authors.Count != 0) addKey(LocalizationKey.SearchFilter.Author, searchFilter.Authors);
        if (searchFilter.BoothIds.Count != 0) addKey(LocalizationKey.SearchFilter.Booth, searchFilter.BoothIds);
        if (searchFilter.SupportedAvatars.Count != 0) addKey(LocalizationKey.SearchFilter.SupportedAvatar, searchFilter.SupportedAvatars);
        if (searchFilter.Categories.Count != 0) addKey(LocalizationKey.SearchFilter.Category, searchFilter.Categories.Select(Localizer.Instance.Get));
        if (searchFilter.ItemMemos.Count != 0) addKey(LocalizationKey.SearchFilter.ItemMemo, searchFilter.ItemMemos);
        if (searchFilter.FolderNames.Count != 0) addKey(LocalizationKey.SearchFilter.FolderName, searchFilter.FolderNames);
        if (searchFilter.FileNames.Count != 0) addKey(LocalizationKey.SearchFilter.FileName, searchFilter.FileNames);
        if (searchFilter.ImplementedAvatars.Count != 0) addKey(LocalizationKey.SearchFilter.ImplementedAvatar, searchFilter.ImplementedAvatars);
        if (searchFilter.NotImplementedAvatars.Count != 0) addKey(LocalizationKey.SearchFilter.NotImplementedAvatar, searchFilter.NotImplementedAvatars);
        if (searchFilter.Tags.Count != 0) addKey(LocalizationKey.SearchFilter.Tag, searchFilter.Tags);
        if (searchFilter.CommonAvatars.Count != 0) addKey(LocalizationKey.SearchFilter.CommonAvatar, searchFilter.CommonAvatars);
        if (searchFilter.SearchWords.Count != 0) addKey(LocalizationKey.SearchFilter.SearchWord, searchFilter.SearchWords);

        string result = toSeparatedString(searchFilterStrings, " / ");
        return Localizer.Instance.Get(LocalizationKey.SearchFilter.Default, result);
    }
}

