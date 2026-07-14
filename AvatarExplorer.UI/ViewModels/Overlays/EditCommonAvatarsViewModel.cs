using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditCommonAvatarsViewModel : ViewModelBase
{
    public event Action? RequestClose;
    
    [Reactive] public IEnumerable<string> Groups { get; set; } = [];
    [Reactive] public int SelectedGroup { get; set; } = 0;
    [Reactive] public string SearchText { get; set; } = string.Empty;

    private Dictionary<int, List<ItemButtonViewModel>> _avatarsByGroup = new();
    public ObservableCollection<ItemButtonViewModel> Avatars { get; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public IReactiveCommand AddGroupCommand { get; }
    public IReactiveCommand RenameGroupCommand { get; }
    public IReactiveCommand RemoveGroupCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand ReplaceAvatarsToGroupCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public void Open()
    {
        // _avatarsByGroup = AvatarExplorerApp.Instance.CommonAvatars.GetAll()
        //     .ToDictionary(c => c.GroupName, c =>
        //     {
        //         return AvatarExplorerApp.Instance.GetItemMaps(c.Avatars).Values
        //             .Select(i => new ItemButtonViewModel(new UISelectableItem(i)))
        //             .ToList();
        //     });
    }

    private void UpdateVisibility()
    {
        if (!_avatarsByGroup.TryGetValue(SelectedGroup, out var items)) return;

        foreach (var item in items)
        {
            // item.IsVisible = item.TagInfo.Value;
        }
    }

    public void UpdateSelectedGroupAvatars()
    {
        
    }
}
