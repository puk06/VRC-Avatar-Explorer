using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class AddItemViewModel : ViewModelBase
{
    public string? ItemId { get; set; } = null;
    public ObservableCollection<string> ItemPaths { get; set; } = [];

    [Reactive] public string BoothUrl { get; set; } = string.Empty;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public int SelectedItemCategory { get; set; } = 0;

    [Reactive] public string SupportedAvatarsText { get; set; } = string.Empty;
    public ObservableCollection<string> SupportedAvatars { get; } = []; // 変更時、もしくはLocalizerの言語変更時にテキストを更新する

    public string Memo { get; set; } = string.Empty;

    [Reactive] public string TagsText { get; set; } = string.Empty;
    public ObservableCollection<string> Tags { get; } = []; // 変更時、もしくはLocalizerの言語変更時にテキストを更新する

    [Reactive] public string AuthorId { get; set; } = string.Empty;
    [Reactive] public string BoothId { get; set; } = string.Empty;
    [Reactive] public string ThumbnailUrl { get; set; } = string.Empty;

    public IReactiveCommand AddFolderCommand { get; }
    public IReactiveCommand AddFileCommand { get; }
    public IReactiveCommand RemovePathCommand { get; }
    public IReactiveCommand FetchBoothDataCommand { get; }
    public IReactiveCommand AddCustomCategoryCommand { get; }
    public IReactiveCommand SelectSupportedAvatarsCommand { get; }
    public IReactiveCommand EditItemMemoCommand { get; }
    public IReactiveCommand EditItemTagsCommand { get; }
    
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }

    public AddItemViewModel()
    {
        
    }

    private void RefleshCategories()
    {
        
    }
}
