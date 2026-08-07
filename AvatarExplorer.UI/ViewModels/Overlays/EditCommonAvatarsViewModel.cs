using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditCommonAvatarsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }

    [Reactive] public ObservableCollection<CommonAvatarViewModel> Groups { get; set; } = [];
    [Reactive] public int SelectedGroupIndex { get; set; } = -1;
    public CommonAvatarViewModel? SelectedGroup => SelectedGroupIndex >= 0 && SelectedGroupIndex < Groups.Count ? Groups[SelectedGroupIndex] : null;

    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    private List<ItemViewModel> _allAvatars = [];

    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand AddGroupCommand { get; }
    public IReactiveCommand RenameGroupCommand { get; }
    public IReactiveCommand RemoveGroupCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand ReplaceAvatarsToGroupCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    private static ItemGroupService ItemService => AvatarExplorerApp.Instance.ItemGroupService;
    private static CommonAvatarRepository CommonAvatarRep => ItemService.CommonAvatarRepository;

    public EditCommonAvatarsViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        AddGroupCommand = ReactiveCommand.CreateFromTask(AddGroup);
        RenameGroupCommand = ReactiveCommand.Create(RenameGroup);
        RemoveGroupCommand = ReactiveCommand.Create(RemoveGroup);
        SelectVisibleCommand = ReactiveCommand.Create(SelectVisible);
        ReplaceAvatarsToGroupCommand = ReactiveCommand.CreateFromTask(ReplaceAvatarsToGroup);
        CloseCommand = ReactiveCommand.Create(Close);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SelectedGroupIndex)
            .Subscribe(_ => UpdateSelectedGroupAvatars());

        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(ApplySearchResult);
    }

    public void Open()
    {
        SelectedGroupIndex = -1;
        RefleshAvatars();
        RefleshGroups();
        SelectedGroupIndex = Groups.Count > 0 ? 0 : -1;
        IsVisible = true;
    }

    private void Close()
    {
        SelectedGroupIndex = -1;
        IsVisible = false;
    }

    private void SelectItem(ItemViewModel item)
    {
        item.IsSelected = !item.IsSelected;
        UpdateGroupAvatars();
    }

    private async Task AddGroup()
    {
        var newGroupName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(newGroupName)) return;

        CommonAvatarRep.Create(newGroupName);
        RefleshGroups();

        SelectedGroupIndex = Groups.Count - 1;
    }

    private async Task RenameGroup()
    {
        var group = SelectedGroup;
        if (group == null) return;
        
        var newGroupName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(newGroupName)) return;

        CommonAvatarRep.RenameGroup(group.Identifier, newGroupName);
        RefleshGroups();
    }

    private async Task RemoveGroup()
    {
        var group = SelectedGroup;
        if (group == null) return;

        var confirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.RemoveCommonAvatarGroup]
        );
        if (!confirmationResult) return;

        var replaceToAvatars = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.EditCommonAvatars.ReplaceGroupToAvatars]
        );
        
        AvatarExplorerApp.Instance.ItemGroupService.RemoveCommonAvatar(group.Identifier, replaceToAvatars);
        RefleshGroups();

        if (Groups.Count == 0) SelectedGroupIndex = -1;
        else if (SelectedGroupIndex >= Groups.Count) SelectedGroupIndex = Groups.Count - 1;
    }

    private void SelectVisible()
    {
        Avatars.ForEach(i => i.IsSelected = true);
        UpdateGroupAvatars();
    }

    private void ApplySearchResult(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Avatars = _allAvatars;
            return;
        }

        var searchQuery = searchText + " OR=true";
        var result = AvatarExplorerApp.Instance.ItemGroupService.SearchItems(searchQuery, SearchResultType.All);
        if (result == null)
        {
            Avatars = _allAvatars;
            return;
        }

        Avatars = _allAvatars.Where(i => result.Contains(i.Identifier)).ToList();
    }

    private async Task ReplaceAvatarsToGroup()
    {
        var group = SelectedGroup;
        if (group == null) return;

        var confirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.EditCommonAvatars.ReplaceAvatarsToGroup]
        );
        if (confirmationResult is false) return;

        ItemService.ReplaceSupportedAvatarsToCommonAvatarGroup(group.Identifier);
    }

    private void RefleshAvatars()
    {
        var avatars = ItemService.GetAvatars(includeCommonAvatar: false, includeTempAvatar: true, rawIdentifier: true);

        _allAvatars = avatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update())
            .ToList();

        Avatars = _allAvatars;
    }

    private void RefleshGroups()
    {
        var groups = CommonAvatarRep.GetAll();

        Groups = new ObservableCollection<CommonAvatarViewModel>(
            groups.Select(i => new CommonAvatarViewModel()
            {
                DisplayName = i.GroupName,
                Identifier = i.Identifier
            })
        );
    }

    private void UpdateSelectedGroupAvatars()
    {
        var group = SelectedGroup;
        if (group == null) return;
        
        var commonAvatar = CommonAvatarRep.Get(group.Identifier);
        if (commonAvatar == null) return;

        _allAvatars.ForEach(i => i.IsSelected = commonAvatar.Avatars.Contains(i.Identifier));
        ApplySearchResult(SearchText);
    }

    private void UpdateGroupAvatars()
    {
        var group = SelectedGroup;
        if (group == null) return;

        CommonAvatarRep.UpdateAvatars(group.Identifier, _allAvatars.Where(i => i.IsSelected).Select(i => i.Identifier));
    }
}
