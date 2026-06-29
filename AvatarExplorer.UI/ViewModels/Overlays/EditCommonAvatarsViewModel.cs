using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditCommonAvatarsViewModel : ViewModelBase
{
    [Reactive] public IEnumerable<string> Groups { get; set; } = [];
    [Reactive] public int SelectedGroup { get; set; } = 0;
    [Reactive] public string SearchText { get; set; } = string.Empty;

    [Reactive] public IEnumerable<ItemButtonViewModel> Avatars { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand AddGroupCommand { get; }
    public IReactiveCommand RenameGroupCommand { get; }
    public IReactiveCommand RemoveGroupCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand ReplaceAvatarsToGroupCommand { get; }
    public IReactiveCommand CloseCommand { get; }
}
