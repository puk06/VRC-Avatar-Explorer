using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditImplementedAvatarsViewModel : ViewModelBase
{
    public event Action? RequestClose;
    
    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    private TaskCompletionSource<string[]?> _tcs = new();

    public IReactiveCommand SelectItemCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }

    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }

    private static ItemGroupService ItemService => AvatarExplorerApp.Instance.ItemGroupService;

    public EditImplementedAvatarsViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        SelectVisibleCommand = ReactiveCommand.Create(SelectVisible);

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

    public void Open()
    {
        RefleshAvatars();
    }

    private void RefleshAvatars()
    {
        var avatars = ItemService.GetAvatars(includeCommonAvatar: false, includeTempAvatar: true);

        Avatars = avatars
            .Select(NavigationItemFactory.CreateFromSelectableItem)
            .Select(i => i.Update());
    }

    public Task<string[]?> WaitForResult()
    {
        _tcs = new();
        return _tcs.Task;
    }
}
