using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.Sort;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Panels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using UIItemSortOrder = AvatarExplorer.UI.Models.Sort.ItemSortOrder;

namespace AvatarExplorer.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Properties
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

    [Reactive] public PanelPageInfo LeftPageInfo { get; set; } = new();
    [Reactive] public PanelPageInfo RightPageInfo { get; set; } = new();

    [Reactive] public bool IsSidePanelVisible { get; set; }
    [Reactive] public int SelectedSidePanelTab { get; set; }
    [Reactive] public double SidePanelMinWidth { get; set; } = 50;
    [Reactive] public double SidePanelMaxWidth { get; set; } = 50;
    [Reactive] public GridLength SidePanelWidth { get; set; } = new(50);

    [Reactive] public Bitmap? HoverThumbnailImage { get; set; }
    [Reactive] public int HoverThumbnailSize { get; set; }
    [Reactive] public bool IsHoverThumbnailVisible { get; set; }
    [Reactive] public PixelPoint HoverThumbnailPosition { get; set; }

    [Reactive] public int SelectedSortOrder { get; set; } = 3;
    [Reactive] public int SelectedSortDirection { get; set; } = 1;
    #endregion

    #region Commands
    public IReactiveCommand UndoCommand { get; }
    public IReactiveCommand HomeCommand { get; }
    public IReactiveCommand OpenSettingsCommand { get; }
    public IReactiveCommand AddItemCommand { get; }
    public IReactiveCommand OpenSidePanelCommand { get; }
    public IReactiveCommand SidePanelButtonPressedCommand { get; }
    public IReactiveCommand SelectLeftItemCommand { get; }
    public IReactiveCommand SelectRightItemCommand { get; }
    public ReactiveCommand<string, Unit> NavigateToSegmentCommand { get; }

    public IReactiveCommand LeftGoFirstCommand { get; }
    public IReactiveCommand LeftGoPrevCommand { get; }
    public IReactiveCommand LeftGoNextCommand { get; }
    public IReactiveCommand LeftGoLastCommand { get; }
    public IReactiveCommand RightGoFirstCommand { get; }
    public IReactiveCommand RightGoPrevCommand { get; }
    public IReactiveCommand RightGoNextCommand { get; }
    public IReactiveCommand RightGoLastCommand { get; }
    public IReactiveCommand ToggleSortDirectionCommand { get; }

    [Reactive] public Material.Icons.MaterialIconKind SortDirectionIcon { get; set; } = Material.Icons.MaterialIconKind.SortDescending;
    #endregion

    #region Fields
    private readonly ItemGroupService _itemGroupService;
    private readonly ItemNavigationService _itemNavigationService;
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private string? _activeSearchQuery;
    private int _lastSelectedLeftCategory;

    private readonly CacheManager<Guid, int> _pageCache = new(0);
    private readonly CacheManager<Guid, Vector> _scrollValueCache = new(AvaloniaVectorUtils.MinValue);
    private readonly CacheManager<int, (int, Vector)> _leftStateCache = new((0, AvaloniaVectorUtils.MinValue));

    private List<ItemViewModel> _allLeftItems = [];
    private List<ItemViewModel> _allMainItems = [];
    private int _normalIconSize = 80;
    private bool _removeBrackets = false;
    #endregion

    #region Constructor
    public MainViewModel()
    {
        Instance = this;

        _itemGroupService = AvatarExplorerApp.Instance.ItemGroupService;
        _itemNavigationService = AvatarExplorerApp.Instance.ItemNavigationService;

        _itemNavigationService.FileOpenRequested += OnFileOpenRequested;

        UndoCommand = ReactiveCommand.Create(Undo);
        HomeCommand = ReactiveCommand.Create(GoHome);
        OpenSettingsCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.SettingsVM.Open());
        AddItemCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.ShowItemEditor());
        SidePanelButtonPressedCommand = ReactiveCommand.Create<int>(SidePanelButtonPressed);
        SelectLeftItemCommand = ReactiveCommand.Create<ItemViewModel>(OnLeftItemSelected);
        SelectRightItemCommand = ReactiveCommand.Create<ItemViewModel>(OnRightItemSelected);
        OpenSidePanelCommand = ReactiveCommand.Create<string>(OpenSidePanel);
        NavigateToSegmentCommand = ReactiveCommand.Create<string>(NavigateToSegment);

        LeftGoFirstCommand = ReactiveCommand.Create(LeftPageInfo.GoFirst);
        LeftGoPrevCommand = ReactiveCommand.Create(LeftPageInfo.GoPrev);
        LeftGoNextCommand = ReactiveCommand.Create(LeftPageInfo.GoNext);
        LeftGoLastCommand = ReactiveCommand.Create(LeftPageInfo.GoLast);
        RightGoFirstCommand = ReactiveCommand.Create(RightPageInfo.GoFirst);
        RightGoPrevCommand = ReactiveCommand.Create(RightPageInfo.GoPrev);
        RightGoNextCommand = ReactiveCommand.Create(RightPageInfo.GoNext);
        RightGoLastCommand = ReactiveCommand.Create(RightPageInfo.GoLast);
        ToggleSortDirectionCommand = ReactiveCommand.Create(ToggleSortDirection);

        LeftPageInfo.WhenAnyValue(x => x.CurrentPage).Subscribe(_ => RefreshLeftItems());
        RightPageInfo.WhenAnyValue(x => x.CurrentPage).Subscribe(_ => RefreshMainItems());

        this.WhenAnyValue(x => x.SelectedSortOrder, x => x.SelectedSortDirection)
            .Skip(1) // 絶対一回は設定されるから
            .Subscribe(tuple =>
            {
                SortDirectionIcon = tuple.Item2 == 0
                    ? Material.Icons.MaterialIconKind.SortAscending
                    : Material.Icons.MaterialIconKind.SortDescending;
                Refresh();
                if (_lastSelectedLeftCategory == (int)QueryType.Avatar) UpdateLeftPanelItems((QueryType)_lastSelectedLeftCategory);
            });

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => RestartSearchTimer());
        AdvancedSearchVM.SearchPropertyChanged += RestartSearchTimer;
        _searchTimer.Tick += OnSearchTimerTick;

        UserPreferencesService.Instance.Repository.OnSettingsChanged += ApplyPreferencesBatch;
        ApplyPreferencesBatch(UserPreferencesService.Instance.Repository.Settings);

        UpdateColumn();
        OnCategoryChanged((int)QueryType.Avatar);
    }
    #endregion

    #region Preferences
    public void ApplyPreferencesBatch(UserPreferences preferences)
    {
        var needsRefresh = false;

        if (SelectedSortOrder != (int)preferences.SortOrder || SelectedSortDirection != (int)preferences.SortDirection)
        {
            SelectedSortOrder = (int)preferences.SortOrder;
            SelectedSortDirection = (int)preferences.SortDirection;
            needsRefresh = true;
        }

        var sizeOrBracketsChanged = false;
        if (_normalIconSize != preferences.NormalIconSize)
        {
            _normalIconSize = preferences.NormalIconSize;
            sizeOrBracketsChanged = true;
        }

        if (_removeBrackets != preferences.RemoveBrackets)
        {
            _removeBrackets = preferences.RemoveBrackets;
            sizeOrBracketsChanged = true;
        }

        if (sizeOrBracketsChanged)
        {
            foreach (var item in _allMainItems.Concat(_allLeftItems))
                item.Update(_normalIconSize, _removeBrackets);
        }

        LeftPageInfo.PageSize = preferences.ItemsPerPage;
        RightPageInfo.PageSize = preferences.ItemsPerPage;

        RefreshLeftItems();
        RefreshMainItems();

        if (needsRefresh) Refresh();
    }

    private void ToggleSortDirection()
    {
        SelectedSortDirection = SelectedSortDirection == 0 ? 1 : 0;
    }
    #endregion

    #region Hover Thumbnail
    public void ShowHoverThumbnail(ItemViewModel item)
    {
        if (item.ViewModelType != ViewModelType.Item || item.Thumbnail == null) return;

        var preferences = UserPreferencesService.Instance.Repository.Settings;
        if (!preferences.EnableHoverIconSize) return;

        HoverThumbnailImage = item.Thumbnail;
        HoverThumbnailSize = preferences.HoverIconSize;
        IsHoverThumbnailVisible = true;
    }

    public void HideHoverThumbnail()
    {
        IsHoverThumbnailVisible = false;
    }

    public void UpdateHoverThumbnailPosition(PixelPoint position)
    {
        HoverThumbnailPosition = position;
    }
    #endregion

    #region Navigation
    public void OnCategoryChanged(int categoryIndex)
    {
        if (!Enum.IsDefined(typeof(QueryType), categoryIndex)) return;

        SaveLeftState(SelectedCategory);
        SelectedCategory = categoryIndex;
        _activeSearchQuery = null;
        SearchText = string.Empty;
        _itemNavigationService.Clear();
        RightPageInfo.Reset();

        UpdateLeftPanelItems((QueryType)categoryIndex);
        RestoreLeftState(categoryIndex);
        _lastSelectedLeftCategory = categoryIndex;
    }

    public async void OnFileOpenRequested(string file)
    {
        if (PathUtils.IsUnitypackageFile(file))
        {
            var itemId = _itemNavigationService.GetCurrentItemId();
            if (itemId == null) return;

            var item = _itemGroupService.ItemRepository.Get(itemId);
            if (item == null) return;

            var category = item.Category;
            var isLocalizable = category.IsLocalizable;
            var displayName = isLocalizable ? Localizer.Instance[item.Category.ToString()] : item.Category.ToString();

            MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Unitypackage.Status.Preparing]);
            var importResult = await UnitypackageService.Import([new() { FilePath = file, CategoryDisplayName = displayName }], onProgress: async (name, percent) =>
            {
                MainWindowViewModel.Instance.ProgressVM.Update(Localizer.Instance.Get(name, percent.ToString()), percent);
            });
            MainWindowViewModel.Instance.ProgressVM.Close();

            if (!importResult.IsError && !string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
            {
                var result = await LauncherService.OpenFile(TopLevelProvider.Current, importResult.ModifiedUnitypackagePath);
                if (result.IsError)
                {
                    MainWindowViewModel.Instance.ShowNotification(
                        Localizer.Instance[Loc.Error.Default],
                        Localizer.Instance[Loc.Error.OpenFileFailed],
                        Avalonia.Controls.Notifications.NotificationType.Error
                    );
                }
            }
            else
            {
                MainWindowViewModel.Instance.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.ImportUnitypackageFailed],
                    Avalonia.Controls.Notifications.NotificationType.Error
                );
            }
        }
        else
        {
            await LauncherService.OpenFile(TopLevelProvider.Current, file);
        }
    }

    public void OpenSidePanel(string index)
    {
        if (!int.TryParse(index, out var selected)) return;

        SelectedSidePanelTab = selected;
        IsSidePanelVisible = true;
        UpdateColumn();
    }
    #endregion

    #region Search
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
            .Cast<Item>();
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
    #endregion

    #region Refresh
    private void Refresh()
    {
        var sortOrder = (UIItemSortOrder)SelectedSortOrder;
        var sortDirection = (SortDirection)SelectedSortDirection;

        if (!string.IsNullOrWhiteSpace(_activeSearchQuery))
        {
            _allMainItems = ItemSortService.Sort(SearchItems(_activeSearchQuery), sortOrder, sortDirection, _removeBrackets)
                .Select(CreateItemViewModel)
                .Select(i => i.Update(_normalIconSize, _removeBrackets))
                .ToList();

            RightPageInfo.TotalItems = _allMainItems.Count;
            RightPageInfo.Reset();
            RefreshMainItems();

            PathSegments = [new PathSegment { DisplayName = Localizer.Instance.Get(Loc.Main.Path.SearchResult, _activeSearchQuery) }];
        }
        else
        {
            var avatarId = _itemNavigationService.GetCurrentAvatarId();
            var commonAvatars = _itemGroupService.CommonAvatarRepository.GetAll();

            var navigationables = _itemNavigationService.GetCurrentSelectionView();
            var items = navigationables.OfType<Item>().ToList();
            var nonItems = navigationables.Where(i => i is not Item).ToList();

            var sortedItems = ItemSortService.Sort(items, sortOrder, sortDirection, _removeBrackets);
            var sortedNavigationables = sortedItems.Cast<INavigationable>().Concat(nonItems);

            _allMainItems = sortedNavigationables
                .Select(i =>
                {
                    var vm = CreateItemViewModel(i);
                    if (i is Item item)
                    {
                        var status = _itemNavigationService.ResolveAvatarStatusForCurrentAvatar(item, avatarId, commonAvatars);
                        if (status.IsOnlyCommon)
                        {
                            var tags = new List<TagViewModel>(item.Tags.Length + 1)
                            {
                                new() { ValueRaw = status.CommonAvatarName, IsCommonAvatar = true }
                            };
                            tags.AddRange(vm.Tags);
                            vm.Tags = tags.ToArray();
                        }
                    }

                    return vm.Update(_normalIconSize, _removeBrackets);
                })
                .ToList();

            RightPageInfo.TotalItems = _allMainItems.Count;
            RefreshMainItems();

            PathSegments = new ObservableCollection<PathSegment>(
                BuildPathSegments(_itemNavigationService.GetCurrentSelectionNodes().Select(i => i.Value)));
        }
    }

    private void RefreshLeftItems()
    {
        LeftItems = LeftPageInfo.GetPageItems(_allLeftItems).ToList();
    }

    private void RefreshMainItems()
    {
        MainItems = RightPageInfo.GetPageItems(_allMainItems).ToList();
    }
    #endregion

    #region Path Segments
    private void NavigateToSegment(string? state)
    {
        if (string.IsNullOrEmpty(state)) return;
        SaveRightState();
        _itemNavigationService.PopToState(state);
        RestoreRightState();
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
            if (stateList[i].StartsWith(ItemNavigationService.ItemPrefix)) segments.Clear();

            var displayName = FormatPathNode(stateList[i]);
            if (string.IsNullOrWhiteSpace(displayName)) continue;

            if (i > 0 && segments.Count != 0)
                segments.Add(new PathSegment { DisplayName = " > " });

            segments.Add(new PathSegment { DisplayName = displayName, State = stateList[i] });
        }

        if (segments.Count == 0)
            segments.Add(new PathSegment { DisplayName = Localizer.Instance[Loc.Main.Path.Placeholder] });

        return segments;
    }
    #endregion

    #region Items
    private static ItemViewModel CreateItemViewModel(INavigationable item)
    {
        var navigationItem = NavigationItemFactory.CreateFromNavigationable(item);
        navigationItem.Actions = ContextMenuCreator.Create(navigationItem.ViewModelType, navigationItem);

        return navigationItem;
    }

    private void UpdateLeftPanelItems(QueryType type)
    {
        var queryItems = _itemGroupService.GetQueryFilters(type);
        if (type == QueryType.Avatar)
        {
            var sortOrder = (UIItemSortOrder)SelectedSortOrder;
            var sortDirection = (SortDirection)SelectedSortDirection;
            queryItems = ItemSortService.SortAvatars(queryItems, sortOrder, sortDirection, _removeBrackets);
        }

        _allLeftItems = queryItems
            .Select(CreateItemViewModel)
            .Select(i => i.Update(_normalIconSize, _removeBrackets))
            .ToList();

        LeftPageInfo.TotalItems = _allLeftItems.Count;
        RefreshLeftItems();
    }
    #endregion

    #region State Cache
    private void SaveLeftState(int categoryIndex)
    {
        _leftStateCache.Add(categoryIndex, (LeftPageInfo.CurrentPage, LeftPageInfo.ScrollOffset));
    }

    private void RestoreLeftState(int categoryIndex)
    {
        if (_leftStateCache.TryGetValue(categoryIndex, out var state))
        {
            LeftPageInfo.CurrentPage = state.Item1;
            LeftPageInfo.ScrollOffset = state.Item2;
            LeftPageInfo.RestoreScrollOffset = state.Item2;
        }
        else
        {
            LeftPageInfo.Reset();
        }
    }

    private void SaveRightState()
    {
        var currentGuid = _itemNavigationService.CurrentStateId;
        if (currentGuid != null)
        {
            _pageCache.Add(currentGuid.Value, RightPageInfo.CurrentPage);
            _scrollValueCache.Add(currentGuid.Value, RightPageInfo.ScrollOffset);
        }
    }

    private void RestoreRightState()
    {
        var currentGuid = _itemNavigationService.CurrentStateId;
        if (currentGuid != null && _pageCache.TryGetValue(currentGuid.Value, out var page))
        {
            RightPageInfo.CurrentPage = page;
            var scroll = _scrollValueCache.Get(currentGuid.Value);
            RightPageInfo.ScrollOffset = scroll;
            RightPageInfo.RestoreScrollOffset = scroll;
        }
        else
        {
            RightPageInfo.Reset();
        }
    }
    #endregion

    #region Selection
    private void OnLeftItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _activeSearchQuery = null;
        SearchText = string.Empty;
        SaveRightState();
        _itemNavigationService.Clear();
        _itemNavigationService.Select(item.Identifier);
        RightPageInfo.Reset();
        Refresh();
    }

    private void OnRightItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _activeSearchQuery = null;
        SearchText = string.Empty;
        SaveRightState();
        _itemNavigationService.PopAllSearchStates();
        _itemNavigationService.Select(item.Identifier);
        RightPageInfo.Reset();
        Refresh();
    }

    private void Undo()
    {
        SaveRightState();
        _itemNavigationService.Undo();
        _activeSearchQuery = null;
        SearchText = string.Empty;
        RestoreRightState();
        Refresh();
    }

    private void GoHome()
    {
        SaveRightState();
        _itemNavigationService.Clear();
        _activeSearchQuery = null;
        SearchText = string.Empty;
        RightPageInfo.Reset();
        Refresh();
    }
    #endregion

    #region Side Panel
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
    #endregion
}
