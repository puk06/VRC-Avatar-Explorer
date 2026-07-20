using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
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

    [Reactive] public string Path { get; set; } = string.Empty;
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

    private readonly ItemGroupService _itemGroupService;
    private readonly ItemNavigationService _itemNavigationService;

    private CacheManager<string, int> _pageCache = new(0);
    private CacheManager<string, Vector> _scrollValueCache = new(AvaloniaVectorUtils.MinValue);


    public MainViewModel()
    {
        Instance = this;

        _itemGroupService = AvatarExplorerApp.Instance.ItemGroupService;
        _itemNavigationService = AvatarExplorerApp.Instance.ItemNavigationService;

        UndoCommand = ReactiveCommand.Create(Undo);
        HomeCommand = ReactiveCommand.Create(GoHome);
        OpenSettingsCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.IsSettingsVisible = true);
        AddItemCommand = ReactiveCommand.Create(() => MainWindowViewModel.Instance.ShowAddItem());
        SidePanelButtonPressedCommand = ReactiveCommand.Create<int>(SidePanelButtonPressed);
        SelectLeftItemCommand = ReactiveCommand.Create<ItemViewModel>(OnLeftItemSelected);
        SelectRightItemCommand = ReactiveCommand.Create<ItemViewModel>(OnRightItemSelected);
        OpenSidePanelCommand = ReactiveCommand.Create<string>(index =>
        {
            if (!int.TryParse(index, out var selected)) return;

            SelectedSidePanelTab = selected;
            IsSidePanelVisible = true;
            UpdateColumn();
        });

        UpdateColumn();
        OnCategoryChanged((int)QueryType.Avatar);
    }

    public void OnCategoryChanged(int categoryIndex)
    {
        if (!Enum.IsDefined(typeof(QueryType), categoryIndex)) return;

        SelectedCategory = categoryIndex;
        _itemNavigationService.Clear();

        UpdateLeftPanelItems((QueryType)categoryIndex);
    }

    private void Refresh()
    {
        MainItems = _itemNavigationService.GetCurrentSelectionView()
            .Select(CreateItemViewModel)
            .Select(i => i.Update());

        Path = BuildLocalizedPath(_itemNavigationService.GetCurrentSelectionNodes().Select(i => i.Value));
    }

    private static string BuildLocalizedPath(IEnumerable<string> states)
    {
        static string FormatPathNode(string state)
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
                return System.IO.Path.GetFileName(value);

            if (prefix == ItemNavigationService.ExtensionPrefix && Enum.TryParse<ItemFileCategoryType>(value, out var extensionCategory))
                return Localizer.Instance[extensionCategory.GetLocalizationKey() ?? value];

            return value;
        }
        var pathNodes = states.Select(FormatPathNode).Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();

        if (pathNodes.Length == 0) return Localizer.Instance[LocalizationKey.Main.Path.Placeholder];
        return string.Join(" > ", pathNodes);
    }

    private static ItemViewModel CreateItemViewModel(INavigationable item)
    {
        var navigationItem = NavigationItemFactory.CreateFromNavigationable(item);
        navigationItem.Actions = ContextMenuCreator.Create(navigationItem.ViewModelType, navigationItem.Identifier);

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

        _itemNavigationService.Clear();
        _itemNavigationService.Select(item.Identifier);
        Refresh();
    }

    private void OnRightItemSelected(ItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Identifier)) return;

        _itemNavigationService.Select(item.Identifier);
        Refresh();
    }

    private void Undo()
    {
        _itemNavigationService.Undo();
        Refresh();
    }

    private void GoHome()
    {
        _itemNavigationService.Clear();
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
