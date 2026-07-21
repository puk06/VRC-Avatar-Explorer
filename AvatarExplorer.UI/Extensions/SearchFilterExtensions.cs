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
        var searchFilterStrings = new List<string>();

        string localize(string key, IEnumerable<string> values) => Localizer.Instance.Get(key, toSeparatedString(values));
        void addKey(string key, IEnumerable<string> values) => searchFilterStrings.Add(localize(key, values));
        string toSeparatedString(IEnumerable<string> values, string separateString = ", ") => string.Join(separateString, values);
        
        IEnumerable<string> getSearchTokensByType(SearchTokenType type, bool localize = false)
        {
            return searchFilter.SearchTokens
                .Where(t => t.Type == type)
                .Select(t => (t.IsNegation ? SearchToken.NegationPrefix : string.Empty) + (localize ? Localizer.Instance[t.Value] : t.Value));
        }

        if (searchFilter.IsOrCondition) searchFilterStrings.Add(Localizer.Instance[Loc.SearchFilter.IsOrSearch]);
        if (searchFilter.IsCategoryOrCondition) searchFilterStrings.Add(Localizer.Instance[Loc.SearchFilter.IsCategoryOrSearch]);
        if (searchFilter.TreatEmptySupportedAvatarAsNone) searchFilterStrings.Add(Localizer.Instance[Loc.SearchFilter.EmptySupportedAvatarAsNone]);
        if (getSearchTokensByType(SearchTokenType.Title).Any()) addKey(Loc.SearchFilter.Title, getSearchTokensByType(SearchTokenType.Title));
        if (getSearchTokensByType(SearchTokenType.Author).Any()) addKey(Loc.SearchFilter.Author, getSearchTokensByType(SearchTokenType.Author));
        if (getSearchTokensByType(SearchTokenType.BoothId).Any()) addKey(Loc.SearchFilter.Booth, getSearchTokensByType(SearchTokenType.BoothId));
        if (getSearchTokensByType(SearchTokenType.SupportedAvatar).Any()) addKey(Loc.SearchFilter.SupportedAvatar, getSearchTokensByType(SearchTokenType.SupportedAvatar));
        if (getSearchTokensByType(SearchTokenType.Category).Any()) addKey(Loc.SearchFilter.Category, getSearchTokensByType(SearchTokenType.Category, true));
        if (getSearchTokensByType(SearchTokenType.ItemMemo).Any()) addKey(Loc.SearchFilter.ItemMemo, getSearchTokensByType(SearchTokenType.ItemMemo));
        if (getSearchTokensByType(SearchTokenType.FolderName).Any()) addKey(Loc.SearchFilter.FolderName, getSearchTokensByType(SearchTokenType.FolderName));
        if (getSearchTokensByType(SearchTokenType.FileName).Any()) addKey(Loc.SearchFilter.FileName, getSearchTokensByType(SearchTokenType.FileName));
        if (getSearchTokensByType(SearchTokenType.ImplementedAvatar).Any()) addKey(Loc.SearchFilter.ImplementedAvatar, getSearchTokensByType(SearchTokenType.ImplementedAvatar));
        if (getSearchTokensByType(SearchTokenType.NotImplementedAvatar).Any()) addKey(Loc.SearchFilter.NotImplementedAvatar, getSearchTokensByType(SearchTokenType.NotImplementedAvatar));
        if (getSearchTokensByType(SearchTokenType.Tag).Any()) addKey(Loc.SearchFilter.Tag, getSearchTokensByType(SearchTokenType.Tag));
        if (getSearchTokensByType(SearchTokenType.CommonAvatar).Any()) addKey(Loc.SearchFilter.CommonAvatar, getSearchTokensByType(SearchTokenType.CommonAvatar));
        if (getSearchTokensByType(SearchTokenType.FreeWord).Any()) addKey(Loc.SearchFilter.SearchWord, getSearchTokensByType(SearchTokenType.FreeWord));

        var result = toSeparatedString(searchFilterStrings, " / ");
        return Localizer.Instance.Get(Loc.SearchFilter.Default, result);
    }
}

