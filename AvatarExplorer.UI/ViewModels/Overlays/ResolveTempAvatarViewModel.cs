using Avalonia.Controls.Notifications;
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

public class ResolveTempAvatarViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    private List<ItemViewModel> _allAvatars = [];

    public IReactiveCommand SelectItemCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    private string? SelectedAvatar { get; set; } = null;

    public ResolveTempAvatarViewModel()
    {
        CancelCommand = ReactiveCommand.Create(Close);
        SelectItemCommand = ReactiveCommand.CreateFromTask<ItemViewModel>(SelectItem);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(ApplySearchResult);
    }

    public void Open(string tempAvatar)
    {
        SelectedAvatar = tempAvatar;
        RefreshAvatars();
        SearchText = string.Empty;
        IsVisible = true;
    }
    private void Close() => IsVisible = false;

    private void RefreshAvatars()
    {
        var avatars = InstanceRepository.ItemGroupService.GetAvatars(includeCommonAvatar: false, includeTempAvatar: false, rawIdentifier: true);
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

    private async Task SelectItem(ItemViewModel item)
    {
        if (SelectedAvatar == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var tempAvatar = InstanceRepository.TempAvatars.Get(SelectedAvatar);
        if (tempAvatar == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var resolveConfirmationResult = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.ResolveTempAvatar, [tempAvatar.AvatarName, item.Title])
        );
        if (!resolveConfirmationResult) return;

        InstanceRepository.ItemGroupService.ResolveTempAvatar(SelectedAvatar, item.Identifier);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.ResolveTempAvatar],
            NotificationType.Success
        );

        IsVisible = false;
    }

    private void ApplySearchResult(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Avatars = _allAvatars;
            return;
        }

        var searchQuery = searchText + " OR=true";
        var result = InstanceRepository.ItemGroupService.SearchItems(searchQuery, SearchResultTypes.All);
        if (result == null)
        {
            Avatars = _allAvatars;
            return;
        }

        Avatars = _allAvatars.Where(i => result.Contains(i.Identifier)).ToList();
    }
}
