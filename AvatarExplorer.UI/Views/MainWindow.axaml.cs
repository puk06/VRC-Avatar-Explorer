using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Models.Navigation;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Models.System;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.Utils;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow : Window
{
    private readonly PageManager _main_pageManager = new();
    private readonly ScrollManager _main_scrollManager = new();

    private string _main_lastSearchTextCache = string.Empty; // 最後に実行された検索のキャッシュ
    private string _main_searchTextCache = string.Empty;
    private bool _main_isLastWindowSearch = false;

    private ItemTagStates _main_lastRightPanelItemTagState = ItemTagStates.None;

    private UserPreferences _userPreferences = new();
    private int ItemsPerPage => _userPreferences.ItemsPerPage;

    private static AvatarExplorerApp AvatarExplorer => AvatarExplorerApp.Instance;
    private static RuntimeSettings RuntimeSettings => AvatarExplorer.GetRuntimeSettings();
    
    private const string Main_GithubApiBaseUrl = "https://api.github.com/users/{0}";

    public MainWindow()
    {
        DataContext = Localizer.Instance;
        AvatarExplorer.Initialize();
        AvatarExplorer.PasswordProvider = Main_GetArchivePasswordAsync;

        InitializeComponent();
        Main_InitializeContextMenuHandlers();

        Main_InitializeTitle();
        Main_InitializeLanguageBox();
        Main_InitializeUserPreferences();

        Main_InitializePipeServer();

        // 設定画面の設定
        SettingsOverlay_SetUiValueFromCurrentSettings();
        SettingsOverlay_ApplySettingsValues(checkDataCopy: false).GetAwaiter().GetResult();

        // 一括インポートプリセットの読み込み
        BulkImportPresetPanel_DrawItemButtons();
    }

    private async ValueTask<string?> Main_GetArchivePasswordAsync(ArchivePasswordRequest request)
    {
        string? password = await ArchivePasswordDialogOverlay_ShowSafeAsync(Path.GetFileName(request.ArchivePath), request.Attempt, request.MaxAttempts);
        if (password == null) return null; // キャンセルされた場合はnullを返す

        return password;
    }

    private async void Main_Loaded(object? sender, RoutedEventArgs e)
    {
        _ = Main_LoadDeveloperProfileIconAsync();

        // 初回起動かチェック
        if (AvatarExplorer.GetAllItems().Length == 0) await InitialSetupOverlay_ShowAsync();

        Main_ReloadCurrentWindow();

        // Scheme & Administrator Mode Check (Windows)
        if (ProcessUtils.IsWindows())
        {
            await Main_CheckSchemeAsync();
            Main_CheckAdministratorMode();
        }

        if (_userPreferences.CheckForUpdate) await UpdateDialogOverlay_CheckAsync(_userPreferences.UpdateChannel);
    }

    private async Task Main_LoadDeveloperProfileIconAsync()
    {
        if (SettingsOverlay_DeveloperProfileImage == null) return;

        try
        {
            string githubOwner = Main_GetRepositoryOwner();
            string profileApiUrl = string.Format(Main_GithubApiBaseUrl, githubOwner);

            using HttpRequestMessage request = new(HttpMethod.Get, profileApiUrl);
            request.Headers.UserAgent.ParseAdd("AvatarExplorer");

            using HttpResponseMessage response = await HttpService.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            await using Stream responseStream = await response.Content.ReadAsStreamAsync();
            using JsonDocument jsonDocument = await JsonDocument.ParseAsync(responseStream);

            if (!jsonDocument.RootElement.TryGetProperty("avatar_url", out JsonElement avatarUrlElement)) return;

            string? avatarUrl = avatarUrlElement.GetString();
            if (string.IsNullOrWhiteSpace(avatarUrl)) return;

            using HttpRequestMessage avatarRequest = new(HttpMethod.Get, avatarUrl);
            avatarRequest.Headers.UserAgent.ParseAdd("AvatarExplorer");

            using HttpResponseMessage avatarResponse = await HttpService.Client.SendAsync(avatarRequest);
            if (!avatarResponse.IsSuccessStatusCode) return;

            await using Stream avatarStream = await avatarResponse.Content.ReadAsStreamAsync();
            SettingsOverlay_DeveloperProfileImage.Source = new Avalonia.Media.Imaging.Bitmap(avatarStream);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to load developer profile icon from GitHub API.", ex);
        }
    }

    private static string Main_GetRepositoryOwner()
    {
        try
        {
            Uri repositoryUri = new(SoftwareLink.RepositoryURL);
            string[] segments = repositoryUri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length > 0 && !string.IsNullOrWhiteSpace(segments[0])) return segments[0];
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to parse repository owner from RepositoryURL.", ex);
        }

        return "puk06";
    }

    public async Task SetApplicationArgs(string[]? args)
    {
        if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0])) return;

        LaunchInfo? launchInfo = LaunchInfoService.GetLaunchInfo(args[0]);
        if (launchInfo == null) return;

        if (launchInfo.AssetPaths.Length != 0 && !string.IsNullOrEmpty(launchInfo.BoothId)) await AddItemOverlay_Open(launchInfo);
    }

    #region Left Panel
    private void Main_RenderLeftPanel()
    {
        if (Main_LeftPanel == null) return;
        Main_LeftPanel.Children.Clear();

        List<ItemCountInfo> items = new();

        ItemTagStates customState = ItemTagStates.None;
        switch (Main_LeftFilter.SelectedIndex)
        {
            case 0:
                {
                    items.AddRange(AvatarExplorer.GetAvatars(includeTempAvatar: true));
                    customState = ItemTagStates.RootAvatar;
                    break;
                }
            case 1:
                {
                    items.AddRange(AvatarExplorer.GetAuthors());
                    customState = ItemTagStates.RootAuthor;
                    break;
                }
            case 2:
                {
                    items.AddRange(AvatarExplorer.GetCategories(includeAllCategory: true));
                    customState = ItemTagStates.RootCategory;
                    break;
                }
        }

        int currentPage = _main_pageManager.GetPage(customState); // -1が返された場合は対応していないStateのため、全てのアイテムを表示してあげる

        foreach (ItemCountInfo itemCountInfo in currentPage != -1 ? items.Skip(currentPage * ItemsPerPage).Take(ItemsPerPage) : items)
        {
            ContextMenu itemContextMenu = ContextMenuFactory.GetContextMenu(ContextMenuCreator.Create(itemCountInfo.Item), Main_ItemButton_ContextMenuItem_Click);
            Button itemButton = ItemButtonFactory.AddItemButton(Main_LeftPanel, new UISelectableItem(itemCountInfo).SetState(customState), RuntimeSettings, _userPreferences, itemContextMenu, LeftPanel_ItemButton_Click);

            // アイテム(アバター)の場合はD&Dイベントを登録してあげる
            if (StateFlagUtils.IsDraggableState(customState)) itemButton.AddHandler(PointerPressedEvent, Main_ItemButton_PointerPressed, RoutingStrategies.Tunnel);
        }

        if (currentPage != -1 && items.Count != 0)
        {
            Main_LeftPanelPageInfo.Children.Clear();
            Panel? pageInfoPanel = PageInfoPanelFactory.CreatePageInfoPanel(customState, currentPage, ItemsPerPage, items.Count, LeftPanel_ItemButton_Click);
            if (pageInfoPanel != null) Main_LeftPanelPageInfo.Children.Add(pageInfoPanel);
        }
        else Main_LeftPanelPageInfo.Children.Clear();

        // スクロール位置をDictionaryから復元してあげる
        Main_LeftPanelScrollViewer.Presenter?.UpdateLayout();
        Main_LeftPanelScrollViewer.Offset = _main_scrollManager.GetScrollValue(customState);
    }
    private void LeftPanel_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        if (button.Tag is ItemTagInfo itemTagInfo)
        {
            AvatarExplorer.SelectClear();
            AvatarExplorer.Select(itemTagInfo.State, itemTagInfo.Value);
            Main_CheckPageStates();
            _main_scrollManager.ResetAllScrollValues(); // 左のパネルのボタンは全てRootのため、スクロール状況を全てリセットしてしまう

            Main_RenderRightPanel();
        }

        if (button.Tag is PageButtonInfo pageButtonInfo)
        {
            _main_pageManager.SetPage(pageButtonInfo.ItemTagState, pageButtonInfo.NextPageValue);
            _main_scrollManager.SetScroll(pageButtonInfo.ItemTagState, new()); // 今のStateのページをリセットしてあげる
            Main_RenderLeftPanel();
        }
    }
    #endregion

    #region Right Panel
    private void Main_RenderRightPanel()
    {
        if (Main_RightPanel == null) return;
        Main_RightPanel.Children.Clear();

        ImmutableArray<ItemCountInfo> items = AvatarExplorer.GetItemsForCurrentState();

        if (items.Length == 0) Main_ShowNoItemsLabel();
        else Main_HideNoItemsLabel();

        ItemTagStates itemTagState = ItemTagStates.None;
        if (items.Length > 0) itemTagState = new UISelectableItem(items[0]).Tag.State;
        _main_lastRightPanelItemTagState = itemTagState;

        int currentPage = _main_pageManager.GetPage(itemTagState); // -1が返された場合は対応していないStateのため、全てのアイテムを表示してあげる

        foreach (ItemCountInfo itemCountInfo in currentPage != -1 ? items.Skip(currentPage * ItemsPerPage).Take(ItemsPerPage) : items)
        {
            ContextMenu itemContextMenu = ContextMenuFactory.GetContextMenu(ContextMenuCreator.Create(itemCountInfo.Item), Main_ItemButton_ContextMenuItem_Click);
            Button itemButton = ItemButtonFactory.AddItemButton(Main_RightPanel, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, itemContextMenu, RightPanel_ItemButton_Click);

            // アイテムの場合はD&Dイベントを登録してあげる
            if (StateFlagUtils.IsDraggableState(itemTagState)) itemButton.AddHandler(PointerPressedEvent, Main_ItemButton_PointerPressed, RoutingStrategies.Tunnel);
        }

        if (currentPage != -1 && items.Length != 0)
        {
            Main_RightPanelPageInfo.Children.Clear();
            Panel? pageInfoPanel = PageInfoPanelFactory.CreatePageInfoPanel(itemTagState, currentPage, ItemsPerPage, items.Length, RightPanel_ItemButton_Click);
            if (pageInfoPanel != null) Main_RightPanelPageInfo.Children.Add(pageInfoPanel);
        }
        else Main_RightPanelPageInfo.Children.Clear();

        _main_isLastWindowSearch = false;
        Main_LoadCurrentPath();
        
        // スクロール位置をDictionaryから復元してあげる
        Main_LeftPanelScrollViewer.Presenter?.UpdateLayout();
        Main_RightPanelScrollViewer.Offset = _main_scrollManager.GetScrollValue(itemTagState);
    }
    private async void RightPanel_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        if (button.Tag is ItemTagInfo itemTagInfo)
        {
            if (itemTagInfo.State == ItemTagStates.ItemFileCategoryOpen) // ファイルを押されると、アイテムを開く処理に移行する
            {
                string itemPath = itemTagInfo.Value; // ItemFileCategoryOpenのValueはファイルのパスになっている
                await Main_OpenFileInternalAsync(itemPath);
            }
            else
            {
                AvatarExplorer.Select(itemTagInfo.State, itemTagInfo.Value);
                Main_CheckPageStates();
                _main_scrollManager.SetScroll(itemTagInfo.State, Main_RightPanelScrollViewer.Offset); // 次の画面に行くため、今のStateのスクロール位置を保存する

                Main_RenderRightPanel();
            }
        }

        if (button.Tag is PageButtonInfo pageButtonInfo)
        {
            _main_pageManager.SetPage(pageButtonInfo.ItemTagState, pageButtonInfo.NextPageValue);
            _main_scrollManager.SetScroll(pageButtonInfo.ItemTagState, new()); // ページは今のStateをリセットしてあげる

            if (pageButtonInfo.ItemTagState == ItemTagStates.SearchItem) Main_ExecuteSearchItems();
            else Main_RenderRightPanel();
        }
    }
    #endregion

    #region Search Box
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private void Main_SearchValue_Changed(object? sender, RoutedEventArgs e)
    {
        // OR検索とカテゴリーOR検索は排他にする
        if (sender == AdvancedSearchPanel_OrSearch && (AdvancedSearchPanel_OrSearch.IsChecked ?? false) && (AdvancedSearchPanel_CategoryOrSearch.IsChecked ?? false))
        {
            AdvancedSearchPanel_CategoryOrSearch.IsChecked = false;
        }
        else if (sender == AdvancedSearchPanel_CategoryOrSearch && (AdvancedSearchPanel_CategoryOrSearch.IsChecked ?? false) && (AdvancedSearchPanel_OrSearch.IsChecked ?? false))
        {
            AdvancedSearchPanel_OrSearch.IsChecked = false;
        }

        _searchTimer.Stop();
        _searchTimer.Tick -= Main_OnSearchTimerTick;
        _searchTimer.Tick += Main_OnSearchTimerTick;
        _searchTimer.Start();
    }
    private void Main_OnSearchTimerTick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        _main_searchTextCache = Main_SearchTextBox.Text ?? string.Empty;
        Main_ExecuteSearchItems();
    }

    private void Main_ExecuteSearchItems(string searchText = "")
    {
        if (!string.IsNullOrEmpty(searchText)) _main_searchTextCache = searchText;

        SearchFilter searchFilter = new();
        AdvancedSearchPanel_ApplyValues(searchFilter);
        SearchFilterBuilder.Build(searchFilter, _main_searchTextCache, SearchUtils.ParseCategory);

        if (string.IsNullOrEmpty(_main_searchTextCache) && searchFilter.SearchTokens.Count == 0)
        {
            Main_RenderRightPanel();
            return;
        }

        // 検索画面に切り替わる時に、前の画面のスクロール位置を保存してあげる
        if (!_main_isLastWindowSearch) _main_scrollManager.SetScroll(_main_lastRightPanelItemTagState, Main_RightPanelScrollViewer.Offset);

        // 検索文字列が前回と違う場合はページ、スクロール位置をリセットする
        if (_main_searchTextCache != _main_lastSearchTextCache)
        {
            _main_pageManager.SetPage(ItemTagStates.SearchItem, 0);
            _main_scrollManager.SetScroll(ItemTagStates.SearchItem, new());
        }
        _main_lastSearchTextCache = _main_searchTextCache;


        Main_RightPanel.Children.Clear();

        ImmutableArray<Item> items = AvatarExplorer.SearchItems(searchFilter);

        if (items.Length == 0) Main_ShowNoItemsLabel();
        else Main_HideNoItemsLabel();

        int currentPage = _main_pageManager.GetPage(ItemTagStates.SearchItem); // SearchItemは必ずページが存在しているため

        foreach (Item item in items.Skip(currentPage * ItemsPerPage).Take(ItemsPerPage))
        {
            ContextMenu itemContextMenu = ContextMenuFactory.GetContextMenu(ContextMenuCreator.Create(item), Main_ItemButton_ContextMenuItem_Click);
            Button itemButton = ItemButtonFactory.AddItemButton(Main_RightPanel, new UISelectableItem(item, 0).SetState(ItemTagStates.SearchItem), RuntimeSettings, _userPreferences, itemContextMenu, RightPanel_ItemButton_Click);

            // D&Dイベントを登録してあげる
            itemButton.AddHandler(PointerPressedEvent, Main_ItemButton_PointerPressed, RoutingStrategies.Tunnel);
        }

        if (items.Length != 0)
        {
            Main_RightPanelPageInfo.Children.Clear();
            Panel? pageInfoPanel = PageInfoPanelFactory.CreatePageInfoPanel(ItemTagStates.SearchItem, currentPage, ItemsPerPage, items.Length, RightPanel_ItemButton_Click);
            if (pageInfoPanel != null) Main_RightPanelPageInfo.Children.Add(pageInfoPanel);
        }
        else Main_RightPanelPageInfo.Children.Clear();

        _main_isLastWindowSearch = true;

        Main_PathTextBox.Text = searchFilter.ToPathString();

        // スクロール位置をDictionaryから復元してあげる
        Main_LeftPanelScrollViewer.Presenter?.UpdateLayout();
        Main_RightPanelScrollViewer.Offset = _main_scrollManager.GetScrollValue(itemTagState);
    }
    #endregion

    #region Path Processing
    private void Main_LoadCurrentPath()
    {
        if (Main_PathTextBox == null) return;

        IEnumerable<SelectionNode> currentSelectionNodes = AvatarExplorer.GetCurrentSelectionNodes();
        if (!currentSelectionNodes.Any())
        {
            Main_PathTextBox.Text = string.Empty;
            return;
        }

        List<SelectionNode> selectionNodes = new();
        foreach (SelectionNode node in currentSelectionNodes)
        {
            if (node.State == ItemTagStates.SearchItem) selectionNodes.Clear();
            selectionNodes.Add(node);
        }

        ImmutableArray<Item> items = AvatarExplorer.GetAllItems();
        ImmutableArray<TempAvatar> tempAvatars = AvatarExplorer.GetAllTempAvatars();
        Main_PathTextBox.Text = string.Join(" > ", selectionNodes.Select(i => PathService.BuildPath(items, tempAvatars, i, RuntimeSettings.RemoveBrackets)));
    }
    #endregion

    #region Main Methods
    private void Main_ExecuteUndo()
    {
        // 選択されていたアイテムが検索結果時のものだったら、キャッシュを元にもう一度検索してあげる
        bool isCurrentSearchNode = AvatarExplorer.GetCurrentNode()?.State == ItemTagStates.SearchItem;

        Main_CheckPageStates(); // SelectUndoより前にやってあげないと、戻った先の画面のページ情報がリセットされる
        if (!_main_isLastWindowSearch) AvatarExplorer.SelectUndo(); // 最後の画面が検索画面だったら、検索だけやめて戻るようにする

        if (isCurrentSearchNode) Main_ExecuteSearchItems();
        else Main_RenderRightPanel();
    }
    private void Main_ExecuteHome()
    {
        AvatarExplorer.SelectClear();
        _main_pageManager.ResetAllPageValues();
        Main_RenderRightPanel();
    }
    private void Main_ReloadCurrentWindow()
    {
        Main_RenderLeftPanel();

        // 最後に表示されていた画面が検索画面だったら、キャッシュを元にもう一度検索してあげる
        if (_main_isLastWindowSearch) Main_ExecuteSearchItems(_main_searchTextCache);
        else
        {
            // 再読込する前に、前の画面のスクロール位置を保存してあげる
            _main_scrollManager.SetScroll(_main_lastRightPanelItemTagState, Main_RightPanelScrollViewer.Offset);
            Main_RenderRightPanel();
        }

        BulkImportPresetPanel_DrawItemButtons();
    }

    private void Main_CheckPageStates()
    {
        List<ItemTagStates> selectedItemTagStates = new();

        foreach (SelectionNode selectionNode in AvatarExplorer.GetCurrentSelectionNodes().Where(i => !selectedItemTagStates.Contains(i.State)))
        {
            selectedItemTagStates.Add(selectionNode.State);
        }

        foreach (var pageInfo in _main_pageManager.GetKeys().Where(i => !selectedItemTagStates.Contains(i)))
        {
            _main_pageManager.ResetPageValue(pageInfo);
        }
    }

    private void Main_ShowNoItemsLabel()
    {
        Main_RightPanelParent.IsVisible = true;
    }
    private void Main_HideNoItemsLabel()
    {
        Main_RightPanelParent.IsVisible = false;
    }

    private async Task Main_OpenFileInternalAsync(string filePath)
    {
        if (PathUtils.IsUnitypackageFile(filePath)) await Main_OpenUnitypackageInternalAsync(filePath); // Unitypackageだと自動展開処理に移る
        else
        {
            ErrorOr<Success> result = await LauncherService.OpenFile(this, filePath);
            if (result.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed]);
        }
    }
    private async Task Main_OpenUnitypackageInternalAsync(string itemPath)
    {
        Item? selectedItem = AvatarExplorer.GetSelectedItem();
        if (selectedItem == null) return;

        ModifiedUnitypackagesResult importResult = await UnitypackageService.Import(
            new Dictionary<string, string>
            {
                { itemPath, selectedItem.Type == ItemType.Custom ? selectedItem.CustomCategory : Localizer.Instance[selectedItem.Type.GetLocalizationKey() ?? selectedItem.Type.ToString()]}
            },
            onProgress: async (name, percent) =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(name, percent.ToString()));
                ProgressOverlay_Update(percent);
            }
        );

        ProgressOverlay_Hide();

        if (!importResult.IsError && !string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
        {
            ErrorOr<Success> result = await LauncherService.OpenFile(this, importResult.ModifiedUnitypackagePath);
            if (result.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed]);
        }
        else
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ImportUnitypackageFailed]);
        }
    }
    #endregion
}
