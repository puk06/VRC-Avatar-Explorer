using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class SelectAvatarsViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public bool AllowTempAvatarCreation { get; set; } = false;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    [Reactive] public string SearchText { get; set; } = string.Empty;
    private TaskCompletionSource<string[]?> _tcs = new();

    public IReactiveCommand SelectItemCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }

    public IReactiveCommand AddTempAvatarCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }

    private static ItemGroupService ItemService => AvatarExplorerApp.Instance.ItemGroupService;

    private bool IncludeCommonAvatar = false;
    private bool IncludeTempAvatar = true;

    public SelectAvatarsViewModel()
    {
        AddTempAvatarCommand = ReactiveCommand.Create(AddTempAvatar);
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        SelectVisibleCommand = ReactiveCommand.Create(SelectVisible);

        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Avatars.Select(i => i.Identifier).ToArray()));
    }

    private void SelectItem(ItemViewModel item)
    {
        item.IsSelected = !item.IsSelected;
    }

    private void SelectVisible()
    {
        Avatars.ForEach(i =>
        {
            if (!i.IsVisible) return;
            i.IsSelected = true;
        });
    }

    public void Open(string title, string[]? avatars = null, bool includeCommonAvatar = false, bool includeTempAvatar = true, bool allowCreateTempAvatar = false)
    {
        Title = title;
        IncludeCommonAvatar = includeCommonAvatar;
        IncludeTempAvatar = includeTempAvatar;
        AllowTempAvatarCreation = allowCreateTempAvatar;

        RefleshAvatars(IncludeCommonAvatar, IncludeTempAvatar);

        if (avatars == null) return;
        Avatars.ForEach(i => i.IsSelected = avatars.Contains(i.Identifier));
    }

    private void RefleshAvatars(bool includeCommonAvatar, bool includeTempAvatar)
    {
        var avatars = ItemService.GetAvatars(includeCommonAvatar, includeTempAvatar);

        Avatars = avatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update());
    }

    private async Task AddTempAvatar()
    {
        var newTempAvatarName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[LocalizationKey.Dialog.Title.NewTempAvatarName]);
        if (string.IsNullOrEmpty(newTempAvatarName)) return;

        AvatarExplorerApp.Instance.TempAvatars.Create(newTempAvatarName);
        RefleshAvatars(IncludeCommonAvatar, IncludeTempAvatar);
    }

    public Task<string[]?> WaitForResult()
    {
        _tcs = new();
        return _tcs.Task;
    }
}
