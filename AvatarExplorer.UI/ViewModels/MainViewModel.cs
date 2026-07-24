using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Panels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    public static MainViewModel Instance { get; private set; } = null!;
    public AdvancedSearchViewModel AdvancedSearchVM { get; } = new();
    public BulkImportViewModel BulkImportVM { get; } = new();
    public BulkImportPresetViewModel BulkImportPresetVM { get; } = new();

    [Reactive] public ObservableCollection<PathSegment> PathSegments { get; set; } = [];
    [Reactive] public bool HasOverflow { get; set; }
    [Reactive] public string SearchText { get; set; } = string.Empty;

    [Reactive] public int SelectedCategory { get; set; }
    [Reactive] public IEnumerable<ItemViewModel> LeftItems { get; set; } = [];
    [Reactive] public IEnumerable<ItemViewModel> MainItems { get; set; } = [];

    [Reactive] public bool IsSidePanelVisible { get; set; }
    [Reactive] public int SelectedSidePanelTab { get; set; }
    [Reactive] public double SidePanelMinWidth { get; set; } = 50;
    [Reactive] public double SidePanelMaxWidth { get; set; } = 50;
    [Reactive] public GridLength SidePanelWidth { get; set; } = new(50);

    public IReactiveCommand UndoCommand { get; }
    public IReactiveCommand HomeCommand { get; }
    public IReactiveCommand OpenSettingsCommand { get; }
    public IReactiveCommand AddItemCommand { get; }
    public IReactiveCommand OpenSidePanelCommand { get; }
    public IReactiveCommand SidePanelButtonPressedCommand { get; }
    public IReactiveCommand SelectLeftItemCommand { get; }
    public IReactiveCommand SelectRightItemCommand { get; }
    public ReactiveCommand<string, Unit> NavigateToSegmentCommand { get; }

    private readonly ItemGroupService _itemGroupService;
    private readonly ItemNavigationService _itemNavigationService;
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private string? _activeSearchQuery;

    private CacheManager<Guid, int> _pageCache = new(0);
    private CacheManager<Guid, Vector> _scrollValueCache = new(AvaloniaVectorUtils.MinValue);

    private int _currentPage = 0;
    private Vector _currentScrollValue = AvaloniaVectorUtils.MinValue;

    public MainViewModel()
    {
        Instance = this;

        _itemGroupService = AvatarExplorerApp.Instance.ItemGroupService;
        _itemNavigationService = AvatarExplorerApp.Instance.ItemNavigationService;

        _itemNavigationService.FileOpenRequested+= (file) =>
        {
            Process.Start("explorer.exe", "/select," + file);
        };

        UndoCommand = ReactiveCommand.Create(Undo);
        HomeCommand = ReactiveCommand.Create(GoHome);
        OpenSettingsCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.SettingsVM.Open());
        AddItemCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.ShowItemEditor());
        SidePanelButtonPressedCommand = ReactiveCommand.Create<int>(SidePanelButtonPressed);
        SelectLeftItemCommand = ReactiveCommand.Create<ItemViewModel>(OnLeftItemSelected);
        SelectRightItemCommand = ReactiveCommand.Create<ItemViewModel>(OnRightItemSelected);
        OpenSidePanelCommand = ReactiveCommand.Create<string>(OpenSidePanel);
        NavigateToSegmentCommand = ReactiveCommand.Create<string>(NavigateToSegment);

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => RestartSearchTimer());
        AdvancedSearchVM.SearchPropertyChanged += RestartSearchTimer;
        _searchTimer.Tick += OnSearchTimerTick;

        UpdateColumn();
        OnCategoryChanged((int)QueryType.Avatar);
        Refresh();
    }

    public void OnCategoryChanged(int categoryIndex)
    {
        if (!Enum.IsDefined(typeof(QueryType), categoryIndex)) return;

        SelectedCategory = categoryIndex;
        _activeSearchQuery = null;
        SearchText = string.Empty;
        _itemNavigationService.Clear();

        UpdateLeftPanelItems((QueryType)categoryIndex);
    }

    public void OpenSidePanel(string index)
    {
        if (!int.TryParse(index, out var selected)) return;

        SelectedSidePanelTab = selected;
        IsSidePanelVisible = true;
        UpdateColumn();
    }

    private void RestartSearchTimer()
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void OnSearchTimerTick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        ExecuteSearch();
    }

    private void Refresh()
    {
        if (!string.IsNullOrWhiteSpace(_activeSearchQuery))
        {
            MainItems = SearchItems(_activeSearchQuery)
                .Select(CreateItemViewModel)
                .Select(i => i.Update());

            PathSegments = [new PathSegment { DisplayName = Localizer.Instance.Get(Loc.Main.Path.SearchResult, _activeSearchQuery) }];
        }
        else
        {
            MainItems = _itemNavigationService.GetCurrentSelectionView()
                .Select(CreateItemViewModel)
                .Select(i => i.Update());

            PathSegments = new ObservableCollection<PathSegment>(
                BuildPathSegments(_itemNavigationService.GetCurrentSelectionNodes().Select(i => i.Value)));
        }
    }

    private void ExecuteSearch()
    {
        var query = BuildSearchString(SearchText, AdvancedSearchVM);
        _activeSearchQuery = string.IsNullOrWhiteSpace(query) ? null : query;
        Refresh();
    }

    private IEnumerable<Item> SearchItems(string query)
    {
        var identifiers = _itemGroupService.SearchItems(query, SearchResultType.Items, SearchUtils.ParseCategory);
        return identifiers
            .Select(_itemGroupService.ItemRepository.Get)
            .Where(item => item != null)
            .Select(item => item!);
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

    private void NavigateToSegment(string? state)
    {
        if (string.IsNullOrEmpty(state)) return;
        _itemNavigationService.PopToState(state);
        Refresh();
    }

    private List<PathSegment> BuildPathSegments(IEnumerable<string> states)
    {
        string FormatPathNode(string state)
        {
            if (!ItemNavigationService.TryParseState(state, out var prefix, out var value)) return state;

            if (prefix == ItemNavigationService.TypePrefix)
            {
                var categoryDisplay = ItemNavigationService.GetCategoryDisplayName(state);
                return Localizer.Instance[categoryDisplay];
            }

            if (prefix == ItemNavigationService.CustomPrefix || prefix == ItemNavigationService.AuthorPrefix)
                return value;

            if (prefix == ItemNavigationService.AvatarPrefix)
                return AvatarExplorerApp.Instance.Items.Get(value)?.Title ?? value;

            if (prefix == ItemNavigationService.ItemPrefix)
                return AvatarExplorerApp.Instance.Items.Get(state)?.Title ?? value;

            if (prefix == ItemNavigationService.FolderPrefix)
                return System.IO.Path.GetFileName(_itemNavigationService.ResolveFolderPath(state) ?? "Unknown Folder");

            if (prefix == ItemNavigationService.ExtensionPrefix && Enum.TryParse<ItemFileCategoryType>(value, out var extensionCategory))
                return Localizer.Instance[extensionCategory.GetLocalizationKey() ?? value];

            return value;
        }

        var segments = new List<PathSegment>();
        var stateList = states.ToList();

        for (int i = 0; i < stateList.Count; i++)
        {
            var displayName = FormatPathNode(stateList[i]);
            if (string.IsNullOrWhiteSpace(displayName)) continue;

            if (i > 0)
                segments.Add(new PathSegment { DisplayName = " > " });

            segments.Add(new PathSegment { DisplayName = displayName, State = stateList[i] });
        }

        if (segments.Count == 0)
            segments.Add(new PathSegment { DisplayName = Localizer.Instance[Loc.Main.Path.Placeholder] });

        return segments;
    }

    private static ItemViewModel CreateItemViewModel(INavigationable item)
    {
        var navigationItem = NavigationItemFactory.CreateFromNavigationable(item);
        navigationItem.Actions = ContextMenuCreator.Create(navigationItem.ViewModelType, navigationItem);

        return navigationItem;
    }

    private void UpdateLeftPanelItems(QueryType type)
    {
        LeftItems = _itemGroupService.GetQueryFilters(type)
            .Select(CreateItemViewModel)
            .Select(i => i.Update());
    }

    private void OnLeftItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _activeSearchQuery = null;
        SearchText = string.Empty;
        _itemNavigationService.Clear();
        var guid = _itemNavigationService.Select(item.Identifier);
        if (guid != null)
        {
            _pageCache.Add((Guid)guid, _currentPage);
            _scrollValueCache.Add((Guid)guid, _currentScrollValue);
        }
        Refresh();
    }

    private void OnRightItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _activeSearchQuery = null;
        SearchText = string.Empty;
        _itemNavigationService.PopAllSearchStates();
        _itemNavigationService.Select(item.Identifier);
        Refresh();
    }

    private void Undo()
    {
        _itemNavigationService.Undo();
        _activeSearchQuery = null;
        SearchText = string.Empty;
        Refresh();
    }

    private void GoHome()
    {
        _itemNavigationService.Clear();
        _activeSearchQuery = null;
        SearchText = string.Empty;
        Refresh();
    }

    private void UpdateColumn()
    {
        SidePanelMinWidth = IsSidePanelVisible ? 342 : 50;
        if (!IsSidePanelVisible)
        {
            SidePanelMaxWidth = 50;
            SidePanelWidth = new(SidePanelMinWidth);
            SidePanelMaxWidth = 550;
        }
    }

    public void SidePanelButtonPressed(int index)
    {
        if (SelectedSidePanelTab != index) return;

        IsSidePanelVisible = false;
        UpdateColumn();
    }
}
