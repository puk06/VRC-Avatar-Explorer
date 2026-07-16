using System;
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

public class EditSupportedAvatarsViewModel : ViewModelBase
{
    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    private TaskCompletionSource<string[]?> _tcs = new();
    
    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand AddTempAvatarCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }

    public EditSupportedAvatarsViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        SelectVisibleCommand = ReactiveCommand.Create(SelectVisible);
        AddTempAvatarCommand = ReactiveCommand.Create(AddTempAvatar);

        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Avatars.Select(i => i.Tag).ToArray()));
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

    public void Open(string[]? avatars = null)
    {
        RefleshAvatars();

        if (avatars != null)
        {
            Avatars.ForEach(i => i.IsSelected = avatars.Contains(i.Tag));
        }
    }

    public Task<string[]?> WaitForResult()
    {
        _tcs = new TaskCompletionSource<string[]?>();
        return _tcs.Task;
    }

    private void RefleshAvatars()
    {
        var itemGroup = AvatarExplorerApp.Instance.ItemGroupService;
        var avatars = itemGroup.GetAvatars(includeCommonAvatar: true, includeTempAvatar: true);

        Avatars = avatars
            .Select(NavigationItemFactory.CreateFromSelectableItem)
            .Select(i => i.Update());
    }

    private async Task AddTempAvatar()
    {
        var newTempAvatarName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[LocalizationKey.Dialog.Title.NewTempAvatarName]);
        if (string.IsNullOrEmpty(newTempAvatarName)) return;

        AvatarExplorerApp.Instance.TempAvatars.Create(newTempAvatarName);
        RefleshAvatars();
    }
}
