using System.Collections.Generic;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditImplementedAvatarsViewModel : ViewModelBase
{
    private string _selectedAvatarId { get; set; } = string.Empty;

    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemButtonViewModel> Avatars { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }
}
