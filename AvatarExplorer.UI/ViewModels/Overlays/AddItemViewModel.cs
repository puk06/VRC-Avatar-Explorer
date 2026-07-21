using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

// TODO: 未完成

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class AddItemViewModel : ViewModelBase
{
    public string? ItemId { get; set; } = null;
    public ObservableCollection<ItemPathViewModel> ItemPaths { get; set; } = [];
    [Reactive] public bool ShouldLinkToOriginal { get; set; } = false;

    [Reactive] public string BoothUrl { get; set; } = string.Empty;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public int SelectedCategoryIndex { get; set; } = 0;
    public ItemCategoryViewModel? SelectedCategory => Categories.Count > SelectedCategoryIndex ? Categories[SelectedCategoryIndex] : null;
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
        CancelCommand = ReactiveCommand.Create(Cancel);
        ConfirmCommand = ReactiveCommand.CreateFromTask(Confirm);
    }

    public void Open(string? itemId = null)
    {
        ItemId = itemId;
        ItemPaths.Clear();

        RefleshCategories();

        if (itemId != null)
        {
            var item = AvatarExplorerApp.Instance.Items.GetById(itemId);
            if (item != null)
            {
                Title = item.Title;
                Author = item.Author;
                AuthorId = item.AuthorId;
                BoothId = item.BoothId.ToString();
                Memo = item.ItemMemo;
                SupportedAvatars = item.SupportedAvatars.ToList();
                Tags = item.Tags.ToList();

                var categoryIndex = Categories.ToList().FindIndex(c => c.Category.Equals(new ItemCategory(item.Type)));
                if (item.Type == ItemType.Custom && !string.IsNullOrEmpty(item.CustomCategory))
                {
                    categoryIndex = Categories.ToList().FindIndex(c => c.Category.Equals(new ItemCategory(item.CustomCategory)));
                }
                SelectedCategoryIndex = categoryIndex >= 0 ? categoryIndex : 0;
            }
        }
        else
        {
            Title = string.Empty;
            Author = string.Empty;
            AuthorId = string.Empty;
            BoothId = string.Empty;
            Memo = string.Empty;
            SupportedAvatars = [];
            Tags = [];
            SelectedCategoryIndex = 0;
        }

        ShouldLinkToOriginal = AvatarExplorerApp.Instance.RuntimeSettings.Settings.ShouldLinkToOriginal;

        UpdateCountField();
    }

    private void Cancel()
    {
        MainWindowViewModel.Instance.IsAddItemVisible = false;
    }

    private async Task Confirm()
    {
        var identifier = string.Empty;
        if (ItemId != null)
        {
            var editContext = new ItemEditContext
            {
                Title = Title,
                Author = Author,
                AuthorId = AuthorId,
                BoothId = ValueParser.Int(BoothId, -1),
                ItemType = SelectedCategory?.Category.Type ?? ItemType.Avatar,
                CustomCategory = SelectedCategory?.Category.Type == ItemType.Custom ? SelectedCategory.Category.CustomCategory ?? string.Empty : string.Empty,
                ItemMemo = Memo
            };
            editContext.SupportedAvatars.AddRange(SupportedAvatars);
            editContext.Tags.AddRange(Tags);

            identifier = $"item:{ItemId}";
            AvatarExplorerApp.Instance.Items.Update(identifier, editContext);
        }
        else
        {
            var creationContext = new ItemCreationContext
            {
                Title = Title,
                Author = Author,
                AuthorId = AuthorId,
                BoothId = int.TryParse(BoothId, out var boothId) ? boothId : -1,
                ItemType = SelectedCategory?.Category.Type ?? ItemType.Avatar,
                CustomCategory = SelectedCategory?.Category.Type == ItemType.Custom ? SelectedCategory.Category.CustomCategory ?? string.Empty : string.Empty,
                ItemMemo = Memo
            };
            creationContext.SupportedAvatars.AddRange(SupportedAvatars);
            creationContext.Tags.AddRange(Tags);

            var item = AvatarExplorerApp.Instance.Items.Create(creationContext);
            identifier = item.Identifier;
        }

        var result = await AvatarExplorerApp.Instance.Items.AddPaths(identifier, ItemPaths.Select(i => i.FullPath), ShouldLinkToOriginal);
        // TODO: resultのハンドリングを追加する

        AvatarExplorerApp.Instance.Items.Save();
        MainWindowViewModel.Instance.IsAddItemVisible = false;
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
        var categories = itemGroupService.GetCategories(includeEmptyCategory: true)
            .Select(i => ResolveCategory(i.Identifier))
            .Where(i => i != null)
            .Cast<ItemCategory>();

        Categories.Clear();
        Categories.AddRange(categories.Select(i => new ItemCategoryViewModel(i).Update()));  // TODO: Localizeする必要がある時はUpdateを実行する);
    }
    private static ItemCategory? ResolveCategory(string groupKey)
    {
        if (!ItemNavigationService.TryParseState(groupKey, out var prefix, out var value)) return null;

        if (prefix == ItemNavigationService.TypePrefix)
        {
            if (ItemNavigationService.TryResolveItemType(value, out var itemType))
            {
                return new(itemType);
            }

            return null;
        }

        if (prefix == ItemNavigationService.CustomPrefix) return new(value);

        return null;
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
            Localizer.Instance[LocalizationKey.SelectAvatars.Title.SupportedAvatars],
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
