using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Panels;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class SearchManager
{
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly ItemGroupService _itemGroupService;
    private readonly Func<string> _getSearchText;
    private readonly Func<AdvancedSearchViewModel> _getAdvancedSearchVM;
    private readonly Action _onSearchExecuted;

    public string? ActiveSearchQuery { get; private set; }

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
        ActiveSearchQuery = string.IsNullOrWhiteSpace(query) ? null : query;
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

        return string.Join(" ", parts);
    }

    private static void AddField(List<string> parts, string fieldName, string value, Func<string, string>? transform = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var transformed = transform?.Invoke(value) ?? value;
        parts.Add($"{fieldName}=\"{transformed}\"");
    }
}
