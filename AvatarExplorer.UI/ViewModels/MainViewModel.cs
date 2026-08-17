using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.Sort;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Managers;
using AvatarExplorer.UI.ViewModels.Panels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    #region Properties
    public AdvancedSearchViewModel AdvancedSearchVM { get; } = new();
    public BulkImportViewModel BulkImportVM { get; } = new();
    public BulkImportPresetViewModel BulkImportPresetVM { get; } = new();

    [Reactive] public ObservableCollection<PathSegment> PathSegments { get; set; } = [];
    [Reactive] public bool HasOverflow { get; set; }
    [Reactive] public string SearchText { get; set; } = string.Empty;

    [Reactive] public int SelectedCategory { get; set; } = 0;
    [Reactive] public IEnumerable<ItemViewModel> LeftItems { get; set; } = [];
    [Reactive] public IEnumerable<ItemViewModel> MainItems { get; set; } = [];
    [Reactive] public bool IsMainItemsEmpty { get; set; }

    [Reactive] public PanelPageInfo LeftPageInfo { get; set; } = new();
    [Reactive] public PanelPageInfo RightPageInfo { get; set; } = new();

    [Reactive] public bool IsSidePanelVisible { get; set; }
    [Reactive] public int SelectedSidePanelTab { get; set; }
    [Reactive] public double SidePanelMinWidth { get; set; } = 50;
    [Reactive] public double SidePanelMaxWidth { get; set; } = 50;
    [Reactive] public GridLength SidePanelWidth { get; set; } = new(50);

    [Reactive] public Bitmap? HoverThumbnailImage { get; set; }
    [Reactive] public bool IsHoverThumbnailVisible { get; set; }
    [Reactive] public PixelPoint HoverThumbnailPosition { get; set; }
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
    public IReactiveCommand NavigateToSegmentCommand { get; }

    public IReactiveCommand LeftGoFirstCommand { get; }
    public IReactiveCommand LeftGoPrevCommand { get; }
    public IReactiveCommand LeftGoNextCommand { get; }
    public IReactiveCommand LeftGoLastCommand { get; }
    public IReactiveCommand RightGoFirstCommand { get; }
    public IReactiveCommand RightGoPrevCommand { get; }
    public IReactiveCommand RightGoNextCommand { get; }
    public IReactiveCommand RightGoLastCommand { get; }
    #endregion

    #region Fields
    private readonly ItemGroupService _itemGroupService;
    private readonly ItemNavigationService _itemNavigationService;
    private readonly HoverThumbnailManager _hoverThumbnailManager;
    private readonly SidePanelManager _sidePanelManager;
    private readonly StateCacheManager _stateCacheManager;
    private readonly SearchManager _searchManager;

    private List<ItemViewModel> _allLeftItems = [];
    private List<ItemViewModel> _allMainItems = [];

    private string? _searchItemBaseState;
    private bool _hasSearchItem;

    private readonly Guid _preLevel0StateGuid = Guid.NewGuid();
    private readonly Guid _preLevel1StateGuid = Guid.NewGuid();
    private bool _isPreviousScreenSearch = false;

    private static UserPreferences UserPreferences => InstanceRepository.UserPreferences;
    #endregion

    #region Constructor
    public MainViewModel()
    {
        UndoCommand = ReactiveCommand.Create(Undo);
        HomeCommand = ReactiveCommand.Create(GoHome);
        OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
        AddItemCommand = ReactiveCommand.Create(OpenItemEditor);
        SidePanelButtonPressedCommand = ReactiveCommand.Create<int>(SidePanelButtonPressed);
        SelectLeftItemCommand = ReactiveCommand.Create<ItemViewModel>(OnLeftItemSelected);
        SelectRightItemCommand = ReactiveCommand.Create<ItemViewModel>(OnRightItemSelected);
        OpenSidePanelCommand = ReactiveCommand.Create<string>(i => OpenSidePanel(ValueParser.Int(i)));
        NavigateToSegmentCommand = ReactiveCommand.Create<string>(NavigateToSegment);

        LeftGoFirstCommand = ReactiveCommand.Create(LeftPageInfo.GoFirst);
        LeftGoPrevCommand = ReactiveCommand.Create(LeftPageInfo.GoPrev);
        LeftGoNextCommand = ReactiveCommand.Create(LeftPageInfo.GoNext);
        LeftGoLastCommand = ReactiveCommand.Create(LeftPageInfo.GoLast);
        RightGoFirstCommand = ReactiveCommand.Create(RightPageInfo.GoFirst);
        RightGoPrevCommand = ReactiveCommand.Create(RightPageInfo.GoPrev);
        RightGoNextCommand = ReactiveCommand.Create(RightPageInfo.GoNext);
        RightGoLastCommand = ReactiveCommand.Create(RightPageInfo.GoLast);

        _itemGroupService = InstanceRepository.ItemGroupService;
        _itemNavigationService = InstanceRepository.NavigationService;

        _hoverThumbnailManager = new HoverThumbnailManager(this);
        _sidePanelManager = new SidePanelManager(this);
        _stateCacheManager = new StateCacheManager(_itemNavigationService);
        _searchManager = new SearchManager(
            _itemGroupService,
            () => SearchText,
            () => AdvancedSearchVM,
            () => Refresh(false)
        );

        IInitializableRegistry.Register(0, (IInitializable)this);
        IInitializableRegistry.Register(0, (IPostInitializable)this);
    }

    public async Task Initialize()
    {
        InitializeSubscriptions();

        Localizer.Instance.LanguageChanged += RefreshAllItems;
        InstanceRepository.UserPreferencesRepository.OnSettingsChanged += _ =>
        {
            UpdateItemsPerPage();
            Refresh();
        };
        _itemNavigationService.FileOpenRequested += OnFileOpenRequested;
        AdvancedSearchVM.SearchPropertyChanged += _searchManager.RestartTimer;
        BulkImportVM.OnItemsChanged += () => OpenSidePanel(1);
    }

    public async Task OnInitialized()
    {
        UpdateItemsPerPage();
        Refresh();
    }

    private void InitializeSubscriptions()
    {
        LeftPageInfo.WhenAnyValue(x => x.CurrentPage).Subscribe(_ => RefreshLeftItems());
        RightPageInfo.WhenAnyValue(x => x.CurrentPage).Subscribe(_ => RefreshMainItems());

        this.WhenAnyValue(i => i.SelectedCategory)
            .Subscribe(_ => UpdateLeftPanelItems());

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => _searchManager.RestartTimer());

        this.WhenAnyValue(x => x.SidePanelWidth)
            .Subscribe(width => _sidePanelManager.OnWidthChanged(width.Value));

        Observable
            .Merge(
                Observable.FromEvent(h => InstanceRepository.Items.OnUpdated += h, h => InstanceRepository.Items.OnUpdated -= h),
                Observable.FromEvent(h => InstanceRepository.CommonAvatars.OnUpdated += h, h => InstanceRepository.CommonAvatars.OnUpdated -= h),
                Observable.FromEvent(h => InstanceRepository.TempAvatars.OnUpdated += h, h => InstanceRepository.TempAvatars.OnUpdated -= h)
            )
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Subscribe(async _ => await Dispatcher.UIThread.InvokeAsync(() => Refresh()));
    }
    #endregion

    #region Hover Thumbnail
    public void ShowHoverThumbnail(ItemViewModel item) => _hoverThumbnailManager.Show(item);
    public void HideHoverThumbnail() => _hoverThumbnailManager.Hide();
    public void UpdateHoverThumbnailPosition(PixelPoint position) => _hoverThumbnailManager.UpdatePosition(position);
    #endregion

    #region Navigation
    public async void OnFileOpenRequested(string file)
    {
        if (PathUtils.IsUnitypackageFile(file))
        {
            var itemId = _itemNavigationService.GetCurrentItemId();
            if (itemId == null)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.FailedToGetCurrentItem],
                    NotificationType.Error
                );
                return;
            }

            var item = InstanceRepository.Items.Get(itemId);
            if (item == null)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.ItemNotFound],
                    NotificationType.Error
                );
                return;
            }

            var category = item.Category;
            var isLocalizable = category.IsLocalizable;
            var displayName = isLocalizable ? Localizer.Instance[item.Category.ToString()] : item.Category.ToString();

            InstanceRepository.MainWindow.ProgressVM.Open(Localizer.Instance[Loc.Processing.Unitypackage.Status.Preparing]);
            var importResult = await UnitypackageService.Import(
                [ new() { FilePath = file, CategoryDisplayName = displayName } ],
                onProgress: async (name, percent) =>
                {
                    InstanceRepository.MainWindow.ProgressVM.Update(
                        Localizer.Instance.Get(name, percent.ToString()),
                        percent
                    );
                }
            );
            InstanceRepository.MainWindow.ProgressVM.Close();

            if (importResult.ContainsScripts)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Warning.Default],
                    Localizer.Instance[Loc.Warning.ScriptsFoundInUnitypackage],
                    NotificationType.Warning
                );
            }

            if (!importResult.IsError && !string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
            {
                var result = await LauncherService.OpenFile(importResult.ModifiedUnitypackagePath);
                if (result.IsError)
                {
                    NotificationManager.Show(
                        Localizer.Instance[Loc.Error.Default],
                        Localizer.Instance[Loc.Error.OpenFileFailed],
                        NotificationType.Error
                    );
                }
            }
            else
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.ImportUnitypackageFailed],
                    NotificationType.Error
                );
            }
        }
        else
        {
            var result = await LauncherService.OpenFile(file);
            if (result.IsError)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.OpenFileFailed],
                    NotificationType.Error
                );
            }
        }
    }
    public void OpenSidePanel(int index) => _sidePanelManager.Open(index);
    #endregion

    #region Refresh
    private void UpdateItemsPerPage()
    {
        var itemsPerPage = UserPreferences.ItemsPerPage;
        LeftPageInfo.PageSize = RightPageInfo.PageSize = itemsPerPage;
    }

    private void Refresh(bool refreshLeftPanelItems = true)
    {
        if (refreshLeftPanelItems) UpdateLeftPanelItems();

        if (!string.IsNullOrWhiteSpace(_searchManager.ActiveSearchQuery))
            RefreshSearchResults(_searchManager.ActiveSearchQuery);
        else
            RefreshNavigationView();
    }

    private void RefreshSearchResults(string searchQuery)
    {
        // 検索前の画面状態を保存
        // 通常画面から検索に入るところ（Lv0）: _preLevel0StateGuid に保存
        // アイテム表示中からファイル検索に入るところ（Lv1）: _preLevel1StateGuid に保存
        if (!_isPreviousScreenSearch)
        {
            if (_hasSearchItem)
                _stateCacheManager.SaveRightStateIfAbsent(RightPageInfo, _preLevel1StateGuid);
            else
                _stateCacheManager.SaveRightStateIfAbsent(RightPageInfo, _preLevel0StateGuid);
        }
        _isPreviousScreenSearch = true;

        var sortOrder = UserPreferences.SortOrder;
        var sortDirection = UserPreferences.SortDirection;
        var isFolderSearchEnabled = UserPreferences.EnableSearchInFolder && _itemNavigationService.GetCurrentItemId() != null;

        if (isFolderSearchEnabled)
        {
            _allMainItems = _itemNavigationService.SearchFilesForCurrentItem(searchQuery)
                .Select(CreateItemViewModel)
                .ToList();
        }
        else
        {
            _allMainItems = ItemSortService
            .Sort(
                _searchManager.SearchItems(searchQuery),
                sortOrder, sortDirection, UserPreferences.RemoveBrackets
            )
            .Select(CreateItemViewModel)
            .ToList();
        }

        RightPageInfo.TotalItems = _allMainItems.Count;

        if (_searchManager.IsRestoring)
        {
            _searchManager.RestorePageInfo(RightPageInfo);
            _searchManager.MarkAsRestored();
        }
        else
        {
            RightPageInfo.Reset();
        }

        RefreshMainItems();

        PathSegments = [new PathSegment
        {
            DisplayName = _searchManager.ActiveSearchQueryDisplayText ?? _searchManager.ActiveSearchQuery ?? ""
        }];
    }

    private void RefreshNavigationView()
    {
        var avatarId = _itemNavigationService.GetCurrentAvatarId();
        var commonAvatars = InstanceRepository.CommonAvatars.GetAll();
        var sortOrder = UserPreferences.SortOrder;
        var sortDirection = UserPreferences.SortDirection;
        var implementedSort = UserPreferences.ImplementedSort;
        var implementedEnabled = implementedSort != ImplementedSort.None;

        var navigationables = _itemNavigationService.GetCurrentSelectionView();
        var items = navigationables.OfType<Item>().ToList();
        var nonItems = navigationables.Where(i => i is not Item).ToList();

        var sortedItems = SortNavigationItems(items, sortOrder, sortDirection, implementedSort, avatarId);
        var sortedNavigationables = sortedItems.Cast<IIdentifiable>().Concat(nonItems);

        _allMainItems = sortedNavigationables
            .Select(nav => CreateItemViewModelWithStatus(nav, avatarId, commonAvatars, implementedEnabled))
            .ToList();

        RightPageInfo.TotalItems = _allMainItems.Count;
        RefreshMainItems();

        PathSegments = new ObservableCollection<PathSegment>(
            BuildPathSegments(_itemNavigationService.GetCurrentSelectionNodes().Select(i => i.Value)));

        if (_isPreviousScreenSearch)
        {
            if (_hasSearchItem)
                _stateCacheManager.RestoreRightState(RightPageInfo, _preLevel1StateGuid);
            else
                _stateCacheManager.RestoreRightState(RightPageInfo, _preLevel0StateGuid);

            _isPreviousScreenSearch = false;
        }
    }

    private static IEnumerable<Item> SortNavigationItems(List<Item> items, ItemSortOrder sortOrder, SortDirection sortDirection, ImplementedSort implementedSort, string? avatarId)
    {
        var sorted = ItemSortService.Sort(items, sortOrder, sortDirection, UserPreferences.RemoveBrackets);

        if (implementedSort == ImplementedSort.None || avatarId == null)
            return sorted;

        var priority = implementedSort == ImplementedSort.Implemented;
        return sorted.OrderByDescending(i => i.ImplementedAvatars.Contains(avatarId) == priority);
    }

    private static ItemViewModel CreateItemViewModelWithStatus(IIdentifiable nav, string? avatarId, IReadOnlyList<CommonAvatar> commonAvatars, bool implementedEnabled)
    {
        var vm = CreateItemViewModel(nav);

        if (nav is not Item item) return vm;

        if (!implementedEnabled || avatarId == null)
        {
            vm.IsImplemented = false;
            vm.IsNotImplemented = false;
        }
        else
        {
            var isImplemented = item.ImplementedAvatars.Contains(avatarId);
            vm.IsImplemented = isImplemented;
            vm.IsNotImplemented = !isImplemented;
        }

        var status = AvatarStatusResolver.Resolve(item, avatarId, commonAvatars);
        if (status.IsOnlyCommon)
        {
            var tags = new List<TagViewModel>(item.Tags.Length + 1)
            {
                new() { ValueRaw = status.CommonAvatarName, IsCommonAvatar = true }
            };
            tags.AddRange(vm.Tags);
            vm.Tags = tags.ToArray();
        }

        return vm;
    }

    private void RefreshAllItems()
    {
        RefreshLeftItems();
        RefreshMainItems();
    }
    private void RefreshLeftItems()
    {
        LeftItems = LeftPageInfo.GetPageItems(_allLeftItems)
            .Select(i => i.Update(UserPreferences.NormalIconSize, UserPreferences.RemoveBrackets))
            .ToList();
    }
    private void RefreshMainItems()
    {
        MainItems = RightPageInfo.GetPageItems(_allMainItems)
            .Select(i => i.Update(UserPreferences.NormalIconSize, UserPreferences.RemoveBrackets))
            .ToList();

        IsMainItemsEmpty = !MainItems.Any();
    }
    #endregion

    #region Path Segments
    private void NavigateToSegment(string? state)
    {
        if (string.IsNullOrEmpty(state)) return;
        _itemNavigationService.PopToState(state);
        _stateCacheManager.RestoreRightState(RightPageInfo);
        Refresh(false);
    }

    private List<PathSegment> BuildPathSegments(IEnumerable<string> states)
    {
        string FormatPathNode(string state)
        {
            if (!ItemNavigationService.TryParseState(state, out var prefix, out var value)) return state;

            if (prefix == ItemNavigationService.AvatarPrefix)
            {
                // Item
                if (value.StartsWith("item"))
                    return InstanceRepository.Items.Get(value)?.Title ?? value;

                // Temp Avatar
                if (value.StartsWith("tempavatar"))
                    return InstanceRepository.TempAvatars.Get(value)?.AvatarName ?? value;

                // Common Avatar (現在は未実装)
                if (value.StartsWith("commonavatar"))
                    return InstanceRepository.CommonAvatars.Get(value)?.GroupName ?? value;

                return value;
            }

            if (prefix == ItemNavigationService.AuthorPrefix)
                return value;
            
            if (prefix == ItemNavigationService.TypePrefix || prefix == ItemNavigationService.CustomPrefix)
            {
                var category = ItemCategory.FromIdentifier(state);
                return category.IsLocalizable ? Localizer.Instance[category.ToString()] : category.ToString();
            }

            if (prefix == ItemNavigationService.ItemPrefix)
                return InstanceRepository.Items.Get(state)?.Title ?? value;

            if (prefix == ItemNavigationService.FolderPrefix)
                return System.IO.Path.GetFileName(_itemNavigationService.ResolvePath(state) ?? "Unknown Folder");

            if (prefix == ItemNavigationService.ExtensionPrefix && Enum.TryParse<ItemFileCategoryType>(value, out var extensionCategory))
                return Localizer.Instance[extensionCategory.GetLocalizationKey() ?? value];

            return value;
        }

        var segments = new List<PathSegment>();
        var stateList = states.ToList();
        var currentItemState = _itemNavigationService.GetCurrentItemId();

        for (int i = 0; i < stateList.Count; i++)
        {
            // 検索状況は前の状況に依存しないため、パスセグメントは検索アイテムのところでリセットする
            var searchResultPrefixFlag = false;
            if (_hasSearchItem && stateList[i] == currentItemState)
            {
                segments.Clear();
                segments.Add(new PathSegment { DisplayName = Localizer.Instance[Loc.Main.Path.SearchResult] });
                searchResultPrefixFlag = true;
            }
            
            var displayName = FormatPathNode(stateList[i]);
            if (string.IsNullOrWhiteSpace(displayName)) continue;

            if ((i > 0 && segments.Count != 0) || searchResultPrefixFlag)
                segments.Add(new PathSegment { DisplayName = " > " });

            segments.Add(new PathSegment { DisplayName = displayName, State = stateList[i] });
        }

        if (segments.Count == 0)
            segments.Add(new PathSegment { DisplayName = Localizer.Instance[Loc.Main.Path.Placeholder] });

        return segments;
    }
    #endregion

    #region Items
    private static ItemViewModel CreateItemViewModel(IIdentifiable item)
    {
        var navigationItem = NavigationItemFactory.CreateFromNavigationable(item);
        navigationItem.Actions = ContextMenuCreator.Create(navigationItem.ViewModelType, navigationItem);

        return navigationItem;
    }

    private int _lastSelectedCategory = -1;
    private void UpdateLeftPanelItems()
    {
        if (_lastSelectedCategory != -1) _stateCacheManager.SaveLeftState(_lastSelectedCategory, LeftPageInfo);

        var type = (QueryType)SelectedCategory;
        var queryItems = _itemGroupService.GetQueryFilters(type);
        if (type == QueryType.Avatar)
        {
            var sortOrder = UserPreferences.SortOrder;
            var sortDirection = UserPreferences.SortDirection;
            var removeBrackets = UserPreferences.RemoveBrackets;
            queryItems = ItemSortService.SortAvatars(queryItems, sortOrder, sortDirection, removeBrackets);
        }

        _allLeftItems = queryItems
            .Select(CreateItemViewModel)
            .ToList();

        LeftPageInfo.TotalItems = _allLeftItems.Count;
        RefreshLeftItems();

        _stateCacheManager.RestoreLeftState(SelectedCategory, LeftPageInfo);
        _lastSelectedCategory = SelectedCategory;
    }
    #endregion

    #region Selection
    private void OnLeftItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _searchManager.ClearQuery();
        _searchManager.ClearSuspendedQuery();
        _hasSearchItem = false;
        _itemNavigationService.Clear();
        _itemNavigationService.Select(item.Identifier);
        RightPageInfo.Reset();
        Refresh(false);
    }

    private void OnRightItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        // ファイルだった場合
        if (item.ViewModelType == ViewModelType.File && item.Identifier.StartsWith(ItemNavigationService.FilePrefix))
        {
            // ファイルはただ選択してイベントを発生させる（選択処理が行われないため、再描画などが必要ない）
            _itemNavigationService.Select(item.Identifier);
            return;
        }

        if (_searchManager.ActiveSearchQuery != null)
        {
            // 検索からアイテム選択: 検索アイテムは1つのみ（上書き）
            if (_hasSearchItem && _searchItemBaseState != null)
            {
                // 前の検索アイテムまでpop
                _itemNavigationService.PopToState(_searchItemBaseState);
            }
            else
            {
                _stateCacheManager.SaveRightState(RightPageInfo);
            }

            _searchItemBaseState = _itemNavigationService.CurrentState?.Value;
            _searchManager.SuspendQuery(RightPageInfo);
            _itemNavigationService.Select(item.Identifier);
            _hasSearchItem = true;

            // B画面に入るので、検索モードを一時的に抜ける
            // これによりRefreshNavigationViewでの復元を防ぎ、ファイル検索時に_preLevel1StateGuidが保存される
            _isPreviousScreenSearch = false;
        }
        else
        {
            _stateCacheManager.SaveRightState(RightPageInfo);
            _itemNavigationService.Select(item.Identifier);
        }

        RightPageInfo.Reset();
        Refresh(false);
    }

    internal void Undo()
    {
        if (_searchManager.ActiveSearchQuery != null)
        {
            // 検索中の Undo: 検索クエリを消して1つ前の画面に戻る
            var wasInsideSearchItem = _hasSearchItem; // Lv1(ファイル検索)中か否か
            _searchManager.ClearQuery();

            if (!wasInsideSearchItem)
            {
                _hasSearchItem = false;
            }
        }
        else
        {
            var popped = _itemNavigationService.Undo();
            if (popped != null)
            {
                var currentState = _itemNavigationService.CurrentState?.Value;
                if (_hasSearchItem && currentState != null && currentState == _searchItemBaseState)
                {
                    // 検索アイテムがpopされた → 検索状態を復元
                    _searchManager.MarkAsRestoring();
                    _searchManager.TryRestoreQuery();
                    _hasSearchItem = false;
                }
                else
                {
                    _stateCacheManager.RestoreRightState(RightPageInfo);
                }
            }
            else
            {
                // はじめの画面なので最初に戻す
                GoHome();
            }
        }

        Refresh(false);
    }

    private void GoHome()
    {
        _itemNavigationService.Clear();
        _searchManager.ClearQuery();
        _searchManager.ClearSuspendedQuery();
        _hasSearchItem = false;
        RightPageInfo.Reset();
        Refresh(false);
    }

    private static void OpenSettings() => InstanceRepository.MainWindow.SettingsVM.Open();
    private static void OpenItemEditor() => InstanceRepository.MainWindow.ItemEditorVM.Open();
    #endregion

    #region Side Panel
    public void SidePanelButtonPressed(int index) => _sidePanelManager.OnButtonPressed(index);
    #endregion
}
