using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.Sort;
using AvatarExplorer.UI.Services.System;
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
    private IEnumerable<ItemViewModel> _allAvatars = [];

    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand AddGroupCommand { get; }
    public IReactiveCommand RenameGroupCommand { get; }
    public IReactiveCommand RemoveGroupCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand UnselectVisibleCommand { get; }
    public IReactiveCommand ReplaceAvatarsToGroupCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public EditCommonAvatarsViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        AddGroupCommand = ReactiveCommand.CreateFromTask(AddGroup);
        RenameGroupCommand = ReactiveCommand.Create(RenameGroup);
        RemoveGroupCommand = ReactiveCommand.Create(RemoveGroup);
        SelectVisibleCommand = ReactiveCommand.Create(() => SetVisibleStatus(true));
        UnselectVisibleCommand = ReactiveCommand.Create(() => SetVisibleStatus(false));
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
        var newGroupName = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(newGroupName)) return;

        InstanceRepository.CommonAvatars.Create(newGroupName);
        RefleshGroups();

        SelectedGroupIndex = Groups.Count - 1;
    }
    private async Task RenameGroup()
    {
        var group = SelectedGroup;
        if (group == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.CommonAvatarNotFound],
                NotificationType.Error
            );
            return;
        }
        
        var newGroupName = await InstanceRepository.MainWindow.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewCommonAvatarGroupName],
            group.DisplayName
        );
        if (string.IsNullOrEmpty(newGroupName)) return;

        InstanceRepository.CommonAvatars.RenameGroup(group.Identifier, newGroupName);
        RefleshGroups();
    }
    private async Task RemoveGroup()
    {
        var group = SelectedGroup;
        if (group == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.CommonAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var confirmationResult = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveCommonAvatarGroup, group.DisplayName)
        );
        if (!confirmationResult) return;

        var replaceToAvatars = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.EditCommonAvatars.ReplaceGroupToAvatars]
        );
        
        InstanceRepository.ItemGroupService.RemoveCommonAvatar(group.Identifier, replaceToAvatars);
        RefleshGroups();

        if (Groups.Count == 0) SelectedGroupIndex = -1;
        else if (SelectedGroupIndex >= Groups.Count) SelectedGroupIndex = Groups.Count - 1;
    }

    private void SetVisibleStatus(bool status)
    {
        Avatars.ForEach(i => i.IsSelected = status);
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
        var result = InstanceRepository.ItemGroupService.SearchItems(searchQuery, SearchResultType.Items | SearchResultType.TempAvatar);
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

        var confirmationResult = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.EditCommonAvatars.ReplaceAvatarsToGroup]
        );
        if (confirmationResult is false) return;

        InstanceRepository.ItemGroupService.ReplaceSupportedAvatarsToCommonAvatarGroup(group.Identifier);
    }

    private void RefleshGroups()
    {
        var lastSelectedGroupIndex = SelectedGroupIndex;
        var groups = InstanceRepository.CommonAvatars.GetAll();

        Groups = new ObservableCollection<CommonAvatarViewModel>(
            groups.Select(i => new CommonAvatarViewModel()
            {
                DisplayName = i.GroupName,
                Identifier = i.Identifier
            })
        );

        SelectedGroupIndex = -1;

        if (Groups.Count == 0) SelectedGroupIndex = -1;
        else if (lastSelectedGroupIndex >= 0 && lastSelectedGroupIndex < Groups.Count) SelectedGroupIndex = lastSelectedGroupIndex;
        else SelectedGroupIndex = 0;
    }
    private void RefleshAvatars()
    {
        var avatars = InstanceRepository.ItemGroupService.GetAvatars(includeCommonAvatar: false, includeTempAvatar: true, rawIdentifier: true);
        var userPreference = InstanceRepository.UserPreferences;
        var sortedAvatars = ItemSortService.SortAvatars(
            avatars,
            userPreference.SortOrder,
            userPreference.SortDirection,
            userPreference.RemoveBrackets,
            rawIdentifier: true
        );

        _allAvatars = sortedAvatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update())
            .ToList();

        Avatars = _allAvatars;
    }

    private void UpdateSelectedGroupAvatars()
    {
        var group = SelectedGroup;
        if (group == null) return;
        
        var commonAvatar = InstanceRepository.CommonAvatars.Get(group.Identifier);
        if (commonAvatar == null) return;

        _allAvatars.ForEach(i => i.IsSelected = commonAvatar.Avatars.Contains(i.Identifier));
        ApplySearchResult(SearchText);
    }
    private void UpdateGroupAvatars()
    {
        var group = SelectedGroup;
        if (group == null) return;

        InstanceRepository.CommonAvatars.UpdateAvatars(
            group.Identifier,
            _allAvatars
                .Where(i => i.IsSelected)
                .Select(i => i.Identifier)
        );
    }
}
