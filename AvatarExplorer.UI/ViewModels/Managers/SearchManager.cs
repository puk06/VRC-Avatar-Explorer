using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Panels;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class SearchManager
{
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly ItemGroupService _itemGroupService;
    private readonly Func<string> _getSearchText;
    private readonly Func<AdvancedSearchViewModel> _getAdvancedSearchVM;
    private readonly Action _onSearchExecuted;

    private string? _suspendedSearchQuery;
    private int _suspendedPage;
    private Vector _suspendedScrollOffset;

    public string? ActiveSearchQuery { get; private set; }
    public bool IsRestoring { get; private set; }
    public bool HasSuspendedQuery => _suspendedSearchQuery != null;

    public string? ActiveSearchQueryDisplayText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ActiveSearchQuery)) return null;
            return FormatSearchQuery(ActiveSearchQuery);
        }
    }

    public SearchManager(
        ItemGroupService itemGroupService,
        Func<string> getSearchText,
        Func<AdvancedSearchViewModel> getAdvancedSearchVM,
        Action onSearchExecuted)
    {
        _itemGroupService = itemGroupService;
        _getSearchText = getSearchText;
        _getAdvancedSearchVM = getAdvancedSearchVM;
        _onSearchExecuted = onSearchExecuted;

        _searchTimer.Tick += OnTimerTick;
    }

    public void RestartTimer()
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    public void ClearQuery()
    {
        ActiveSearchQuery = null;
    }

    public void SuspendQuery(PanelPageInfo rightPageInfo)
    {
        if (ActiveSearchQuery == null) return;
        _suspendedSearchQuery = ActiveSearchQuery;
        _suspendedPage = rightPageInfo.CurrentPage;
        _suspendedScrollOffset = rightPageInfo.ScrollOffset;
        ActiveSearchQuery = null;
    }

    public bool TryRestoreQuery()
    {
        if (_suspendedSearchQuery == null) return false;

        ActiveSearchQuery = _suspendedSearchQuery;
        _suspendedSearchQuery = null;
        return true;
    }

    public void RestorePageInfo(PanelPageInfo rightPageInfo)
    {
        if (!IsRestoring) return; // Only restore if we are in the restoring state
        rightPageInfo.CurrentPage = _suspendedPage;
        rightPageInfo.RestoreScrollOffset = _suspendedScrollOffset;
    }

    public void ClearSuspendedQuery()
    {
        _suspendedSearchQuery = null;
    }

    public void MarkAsRestored() => IsRestoring = false;
    public void MarkAsRestoring() => IsRestoring = true;

    public IEnumerable<Item> SearchItems(string query)
    {
        var identifiers = _itemGroupService.SearchItems(query, SearchResultType.Items, SearchUtils.ParseCategory);
        return identifiers
            .Select(_itemGroupService.ItemRepository.Get)
            .Where(item => item != null)
            .Cast<Item>();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        ExecuteSearch();
    }

    private void ExecuteSearch()
    {
        var query = BuildSearchString(_getSearchText(), _getAdvancedSearchVM());
        var parsed = SearchQueryParser.Parse(query);
        ActiveSearchQuery = parsed.Tokens.Count == 0 ? null : query;
        _suspendedSearchQuery = null;
        _onSearchExecuted();
    }

    private static string BuildSearchString(string searchText, AdvancedSearchViewModel advancedSearch)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchText))
            parts.Add(searchText);

        AddField(parts, "Title", advancedSearch.Title);
        AddField(parts, "Author", advancedSearch.Author);
        AddField(parts, "BoothId", advancedSearch.BoothId);
        AddField(parts, "SupportedAvatar", advancedSearch.SupportedAvatar);
        AddField(parts, "Category", advancedSearch.Category, value => Localizer.Instance.GetKey(value) ?? value);
        AddField(parts, "Memo", advancedSearch.Memo);
        AddField(parts, "ImplementedAvatar", advancedSearch.ImplementedAvatar);
        AddField(parts, "NotImplementedAvatar", advancedSearch.NotImplementedAvatar);
        AddField(parts, "Tag", advancedSearch.Tag);
        AddField(parts, "CommonAvatar", advancedSearch.CommonAvatar);

        if (advancedSearch.IsOr)
            parts.Add("OR=true");

        return string.Join(" ", parts);
    }

    private static void AddField(List<string> parts, string fieldName, string value, Func<string, string>? transform = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var tokens = TextParser.Parse(value);
        foreach (var token in tokens)
        {
            var isNegation = token.StartsWith('~');
            var actualValue = isNegation ? token[1..] : token;
            var transformed = transform?.Invoke(actualValue) ?? actualValue;
            var prefix = isNegation ? "~" : "";
            parts.Add($"{prefix}{fieldName}=\"{transformed}\"");
        }
    }

    private static string FormatSearchQuery(string query)
    {
        var parsed = SearchQueryParser.Parse(query);
        if (parsed.Tokens.Count == 0) return query;

        const string connector = " / ";
        const string valueSeparator = ", ";
        var negationPrefix = Localizer.Instance.Get(Loc.SearchFilter.NegationPrefix);

        var grouped = parsed.Tokens
            .GroupBy(t => new { Field = t.Field?.ToLowerInvariant(), t.IsNegation })
            .Select(g =>
            {
                var field = g.Key.Field;
                var isNegation = g.Key.IsNegation;
                var values = g.Select(t => t.Value);
                var valuesStr = string.Join(valueSeparator, values);

                var locKey = field switch
                {
                    "title" => Loc.SearchFilter.Title,
                    "author" => Loc.SearchFilter.Author,
                    "boothid" or "booth" => Loc.SearchFilter.Booth,
                    "supportedavatar" => Loc.SearchFilter.SupportedAvatar,
                    "category" => Loc.SearchFilter.Category,
                    "memo" => Loc.SearchFilter.ItemMemo,
                    "implementedavatar" => Loc.SearchFilter.ImplementedAvatar,
                    "notimplementedavatar" => Loc.SearchFilter.NotImplementedAvatar,
                    "tag" => Loc.SearchFilter.Tag,
                    "commonavatar" => Loc.SearchFilter.CommonAvatar,
                    _ => null
                };

                var formatted = locKey != null
                    ? Localizer.Instance.Get(locKey, valuesStr)
                    : Localizer.Instance.Get(Loc.SearchFilter.SearchWord, valuesStr);

                return isNegation ? negationPrefix + formatted : formatted;
            });

        var result = string.Join(connector, grouped);

        if (parsed.IsOr)
            result = $"({Localizer.Instance.Get(Loc.SearchFilter.IsOrSearch)}) {result}";

        return Localizer.Instance.Get(Loc.SearchFilter.Default, result);
    }
}
