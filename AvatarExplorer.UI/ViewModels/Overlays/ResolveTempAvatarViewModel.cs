using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
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

    private static ItemGroupService ItemService => AvatarExplorerApp.Instance.ItemGroupService;

    public ResolveTempAvatarViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
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

    private void RefreshAvatars()
    {
        var avatars = ItemService.GetAvatars(includeCommonAvatar: false, includeTempAvatar: true, rawIdentifier: true);

        _allAvatars = avatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update())
            .ToList();

        Avatars = _allAvatars;
    }

    private void ApplySearchResult(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Avatars = _allAvatars;
            return;
        }

        var searchQuery = searchText + " OR=true";
        var result = ItemService.SearchItems(searchQuery, SearchResultType.All);
        if (result == null)
        {
            Avatars = _allAvatars;
            return;
        }

        Avatars = _allAvatars.Where(i => result.Contains(i.Identifier)).ToList();
    }

    private async Task SelectItem(ItemViewModel item)
    {
        if (SelectedAvatar == null)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var tempAvatar = AvatarExplorerApp.Instance.TempAvatars.Get(SelectedAvatar);
        if (tempAvatar == null)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var resolveConfirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.ResolveTempAvatar, [tempAvatar.AvatarName, item.Title])
        );
        if (!resolveConfirmationResult) return;

        AvatarExplorerApp.Instance.ItemGroupService.ResolveTempAvatar(SelectedAvatar, item.Identifier);
        IsVisible = false;
    }
}
