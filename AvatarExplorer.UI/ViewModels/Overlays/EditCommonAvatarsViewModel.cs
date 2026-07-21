using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditCommonAvatarsViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }

    [Reactive] public IEnumerable<CommonAvatarViewModel> Groups { get; set; } = [];
    [Reactive] public CommonAvatarViewModel? SelectedGroup { get; set; } = null;

    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];

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
        CloseCommand = ReactiveCommand.Create(() => IsVisible = false);

        this.WhenAnyValue(i => i.SelectedGroup)
            .Subscribe(i => UpdateSelectedGroupAvatars());
    }

    public void Open()
    {
        RefleshAvatars();
        RefleshGroups();
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
    }

    private async Task RenameGroup()
    {
        if (SelectedGroup == null) return;
        
        var newGroupName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(newGroupName)) return;

        CommonAvatarRep.RenameGroup(SelectedGroup.Identifier, newGroupName);
        RefleshGroups();
    }

    private async Task RemoveGroup()
    {
        if (SelectedGroup == null) return;

        var confirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.RemoveCommonAvatarGroup]
        );
        if (confirmationResult is false) return;
        
        CommonAvatarRep.Remove(SelectedGroup.Identifier);
        RefleshGroups();
    }

    private void SelectVisible()
    {
        Avatars.ForEach(i =>
        {
            if (!i.IsVisible) return;
            i.IsSelected = true;
        });
    }

    private async Task ReplaceAvatarsToGroup()
    {
        if (SelectedGroup == null) return;

        var confirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.EditCommonAvatars.ReplaceAvatarsToGroup]
        );
        if (confirmationResult is false) return;

        ItemService.ReplaceSupportedAvatarsToCommonAvatarGroup(SelectedGroup.Identifier);
    }

    private void RefleshAvatars()
    {
        var avatars = ItemService.GetAvatars(includeCommonAvatar: true, includeTempAvatar: true);

        Avatars = avatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update());
    }

    private void RefleshGroups()
    {
        var groups = CommonAvatarRep.GetAll();

        Groups = groups.Select(i => new CommonAvatarViewModel()
        {
            DisplayName = i.GroupName,
            Identifier = i.Identifier
        });
    }

    private void UpdateSelectedGroupAvatars()
    {
        if (SelectedGroup == null) return;
        
        var group = CommonAvatarRep.Get(SelectedGroup.Identifier);
        if (group == null) return;

        Avatars.ForEach(i => i.IsSelected = group.Avatars.Contains(i.Identifier));
    }

    private void UpdateGroupAvatars()
    {
        if (SelectedGroup == null) return;

        CommonAvatarRep.UpdateAvatars(SelectedGroup.Identifier, Avatars.Where(i => i.IsSelected).Select(i => i.Identifier));
    }
}
