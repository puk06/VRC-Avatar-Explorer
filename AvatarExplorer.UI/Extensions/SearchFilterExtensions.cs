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
        IEnumerable<string> getSearchTokensByType(SearchTokenType type) => searchFilter.SearchTokens.Where(t => t.Type == type).Select(t => t.Value);

        if (searchFilter.IsOrCondition) searchFilterStrings.Add(Localizer.Instance[LocalizationKey.SearchFilter.IsOrSearch]);
        if (getSearchTokensByType(SearchTokenType.Title).Any()) addKey(LocalizationKey.SearchFilter.Title, getSearchTokensByType(SearchTokenType.Title));
        if (getSearchTokensByType(SearchTokenType.Author).Any()) addKey(LocalizationKey.SearchFilter.Author, getSearchTokensByType(SearchTokenType.Author));
        if (getSearchTokensByType(SearchTokenType.BoothId).Any()) addKey(LocalizationKey.SearchFilter.Booth, getSearchTokensByType(SearchTokenType.BoothId));
        if (getSearchTokensByType(SearchTokenType.SupportedAvatar).Any()) addKey(LocalizationKey.SearchFilter.SupportedAvatar, getSearchTokensByType(SearchTokenType.SupportedAvatar));
        if (getSearchTokensByType(SearchTokenType.Category).Any()) addKey(LocalizationKey.SearchFilter.Category, getSearchTokensByType(SearchTokenType.Category).Select(Localizer.Instance.Get));
        if (getSearchTokensByType(SearchTokenType.ItemMemo).Any()) addKey(LocalizationKey.SearchFilter.ItemMemo, getSearchTokensByType(SearchTokenType.ItemMemo));
        if (getSearchTokensByType(SearchTokenType.FolderName).Any()) addKey(LocalizationKey.SearchFilter.FolderName, getSearchTokensByType(SearchTokenType.FolderName));
        if (getSearchTokensByType(SearchTokenType.FileName).Any()) addKey(LocalizationKey.SearchFilter.FileName, getSearchTokensByType(SearchTokenType.FileName));
        if (getSearchTokensByType(SearchTokenType.ImplementedAvatar).Any()) addKey(LocalizationKey.SearchFilter.ImplementedAvatar, getSearchTokensByType(SearchTokenType.ImplementedAvatar));
        if (getSearchTokensByType(SearchTokenType.NotImplementedAvatar).Any()) addKey(LocalizationKey.SearchFilter.NotImplementedAvatar, getSearchTokensByType(SearchTokenType.NotImplementedAvatar));
        if (getSearchTokensByType(SearchTokenType.Tag).Any()) addKey(LocalizationKey.SearchFilter.Tag, getSearchTokensByType(SearchTokenType.Tag));
        if (getSearchTokensByType(SearchTokenType.CommonAvatar).Any()) addKey(LocalizationKey.SearchFilter.CommonAvatar, getSearchTokensByType(SearchTokenType.CommonAvatar));
        if (getSearchTokensByType(SearchTokenType.FreeWord).Any()) addKey(LocalizationKey.SearchFilter.SearchWord, getSearchTokensByType(SearchTokenType.FreeWord));

        string result = toSeparatedString(searchFilterStrings, " / ");
        return Localizer.Instance.Get(LocalizationKey.SearchFilter.Default, result);
    }
}

