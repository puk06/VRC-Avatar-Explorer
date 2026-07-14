using System.Collections.Generic;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ResolveTempAvatarViewModel : ViewModelBase
{
    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemButtonViewModel> Avatars { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }
    
    public IReactiveCommand ResolveCommand { get; }
    public IReactiveCommand CloseCommand { get; }
}
