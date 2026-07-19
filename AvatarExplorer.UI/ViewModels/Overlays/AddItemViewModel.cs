using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class AddItemViewModel : ViewModelBase
{
    public string? ItemId { get; set; } = null;
    public ObservableCollection<ItemPathViewModel> ItemPaths { get; set; } = [];

    [Reactive] public string BoothUrl { get; set; } = string.Empty;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public int SelectedItemCategoryIndex { get; set; } = 0;
    public ObservableCollection<ItemCategoryViewModel> Categories { get; set; } = [];

    [Reactive] public string SupportedAvatarsText { get; set; } = string.Empty;
    public IEnumerable<string> SupportedAvatars { get; set; } = []; // 変更時、もしくはLocalizerの言語変更時にテキストを更新する

    public string Memo { get; set; } = string.Empty;

    [Reactive] public string TagsText { get; set; } = string.Empty;
    public IEnumerable<string> Tags { get; set; } = []; // 変更時、もしくはLocalizerの言語変更時にテキストを更新する

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
        AddFolderCommand = ReactiveCommand.CreateFromTask(SelectAndAddFolders);
        AddFileCommand = ReactiveCommand.CreateFromTask(SelectAndAddFiles);
        RemovePathCommand = ReactiveCommand.Create<ItemPathViewModel>(RemovePath);
        FetchBoothDataCommand = ReactiveCommand.CreateFromTask(FetchBoothData);
        AddCustomCategoryCommand = ReactiveCommand.CreateFromTask(AddCustomCategory);
        SelectSupportedAvatarsCommand = ReactiveCommand.CreateFromTask(SelectSupportedAvatars);
        EditItemMemoCommand = ReactiveCommand.CreateFromTask(EditItemMemo);
        EditItemTagsCommand = ReactiveCommand.CreateFromTask(EditItemTags);
    }

    public void Open(string? itemId = null)
    {
        ItemId = itemId;

        RefleshCategories();
        SelectedItemCategoryIndex = 0;

        UpdateCountField();
    }

    private async Task SelectAndAddFolders()
    {
        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath],
            allowMultiple: true
        );

        if (folders == null || folders.Length == 0) return;

        ItemPaths.AddRange(folders.Select(i => new ItemPathViewModel(i, ItemPathType.Folder)));
    }

    private async Task SelectAndAddFiles()
    {
        var files = await StorageService.OpenFileDialog(
            TopLevelProvider.Current,
            Localizer.Instance[LocalizationKey.Dialog.SelectFilePath],
            allowMultiple: true
        );

        if (files == null || files.Length == 0) return;

        ItemPaths.AddRange(files.Select(i => new ItemPathViewModel(i, ItemPathType.File)));
    }
    private async Task FetchBoothData()
    {
        var fetchResult = await BoothService.Fetch(BoothUrl, waitCooldown: true);
        if (fetchResult.IsError)
        {
            // TODO: MainWindowViewModel.Instance 通知
            return;
        }

        var boothData = fetchResult.Value;
        Title = boothData.Title;
        Author = boothData.Shop.Name;
        // TODO: SelectedItemCategory = Categoryを設定（これはCategoryViewModelみたいなのでやっても良いかも、正直あんまり良い実装が思いついてない）
        AuthorId = boothData.Shop.Id;
        BoothId = boothData.BoothId.ToString();
        ThumbnailUrl = boothData.ThumbnailUrl;
    }

    private void RemovePath(ItemPathViewModel pathModel)
    {
        ItemPaths.Remove(pathModel);
    }

    private void RefleshCategories()
    {
        var itemGroupService = AvatarExplorerApp.Instance.ItemGroupService;
        var categories = itemGroupService.GetCategories(includeEmptyCategory: true).Select(i => ResolveCategory(i.Identifier));

        Categories.Clear();
        Categories.AddRange(categories.Select(i => new ItemCategoryViewModel(i).Update()));  // TODO: Localizeする必要がある時はUpdateを実行する);
    }
    private static ItemCategory ResolveCategory(string groupKey)
    {
        if (!ItemNavigationService.TryParseState(groupKey, out var prefix, out var value)) return new(ItemType.Avatar);

        if (prefix == ItemNavigationService.TypePrefix)
        {
            if (ItemNavigationService.TryResolveItemType(value, out var itemType))
            {
                return new(itemType);
            }

            return new(ItemType.Avatar);
        }

        if (prefix == ItemNavigationService.CustomPrefix) return new(value);

        return new(ItemType.Avatar);
    }

    private async Task AddCustomCategory()
    {
        var newCategory = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[LocalizationKey.Dialog.Title.NewCustomCategoryName]);
        if (string.IsNullOrEmpty(newCategory)) return;

        Categories.Add(new ItemCategoryViewModel(new ItemCategory(newCategory)).Update());
    }

    private async Task SelectSupportedAvatars()
    {
        var avatars = await MainWindowViewModel.Instance.ShowSelectAvatars(
            SupportedAvatars.ToArray(),
            includeCommonAvatar: true,
            includeTempAvatar: true,
            allowCreateTempAvatar: true
        );
        if (avatars == null) return;

        SupportedAvatars = avatars;
        UpdateCountField();
    }
    private int GetSupportedAvatarsCount()
    {
        var itemGroup = AvatarExplorerApp.Instance.ItemGroupService;
        return itemGroup.GetAllSupportedAvatarsIds(SupportedAvatars, true).Length;
    }

    private async Task EditItemMemo()
    {
        var newMemo = await MainWindowViewModel.Instance.ShowEditMemoDialog(Memo);
        if (newMemo == null) return;

        Memo = newMemo;
    }

    private async Task EditItemTags()
    {
        var newTags = await MainWindowViewModel.Instance.ShowEditTagsDialog(Tags.ToArray());
        if (newTags == null) return;

        Tags = newTags;
        UpdateCountField();
    }

    private void UpdateCountField()
    {
        TagsText = Localizer.Instance.Get(LocalizationKey.AddItem.SelectedTagsCount, Tags.Count().ToString());
        SupportedAvatarsText = Localizer.Instance.Get(LocalizationKey.AddItem.SelectedAvatarsCount, GetSupportedAvatarsCount().ToString());
    }
}
