using System.Collections.Generic;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditSupportedAvatarsViewModel : ViewModelBase
{
    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemButtonViewModel> Avatars { get; set; } = [];
    
    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand AddTempAvatarCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }
}
