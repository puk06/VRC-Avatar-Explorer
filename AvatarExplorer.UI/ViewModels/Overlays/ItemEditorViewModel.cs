using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.System;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ItemEditorViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; } = false;
    public string? Identifier { get; set; } = null;
    public ObservableCollection<ItemPathViewModel> ItemPaths { get; set; } = [];
    [Reactive] public bool ShouldLinkToOriginal { get; set; } = false;

    [Reactive] public string BoothUrl { get; set; } = string.Empty;
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public int SelectedCategoryIndex { get; set; } = 0;
    public ItemCategoryViewModel? SelectedCategory => Categories.Count > SelectedCategoryIndex ? Categories[SelectedCategoryIndex] : null;
    [Reactive] public ObservableCollection<ItemCategoryViewModel> Categories { get; set; } = [];

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
    public IReactiveCommand AddUrlCommand { get; }
    public IReactiveCommand RemovePathCommand { get; }
    public IReactiveCommand FetchBoothDataCommand { get; }
    public IReactiveCommand AddCustomCategoryCommand { get; }
    public IReactiveCommand SelectSupportedAvatarsCommand { get; }
    public IReactiveCommand EditItemMemoCommand { get; }
    public IReactiveCommand EditItemTagsCommand { get; }
    
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }

    public ItemEditorViewModel()
    {
        AddFolderCommand = ReactiveCommand.CreateFromTask(SelectAndAddFolders);
        AddFileCommand = ReactiveCommand.CreateFromTask(SelectAndAddFiles);
        AddUrlCommand = ReactiveCommand.CreateFromTask(AddUrl);
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
        Identifier = itemId;
        ItemPaths.Clear();

        RefleshCategories();

        if (itemId != null)
        {
            var item = AvatarExplorerApp.Instance.Items.Get(itemId);
            if (item != null)
            {
                BoothUrl = item.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
                Title = item.Title;
                Author = item.Author;
                AuthorId = item.AuthorId;
                BoothId = item.BoothId.ToString();
                Memo = item.ItemMemo;
                SupportedAvatars = item.SupportedAvatars.ToList();
                Tags = item.Tags.ToList();

                var categoryIndex = GetCategoryIndex(item.Category);
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
            SelectedCategoryIndex = -1;
            SelectedCategoryIndex = 0;
        }

        ShouldLinkToOriginal = AvatarExplorerApp.Instance.RuntimeSettings.Settings.ShouldLinkToOriginal;

        UpdateCountField();
        IsVisible = true;
    }
    public async void Open(LaunchInfo launchInfo)
    {
        if (IsVisible && BoothId == launchInfo.BoothId)
        {
            AddPaths(launchInfo.AssetPaths);
            return;
        }

        Identifier = null;
        ItemPaths.Clear();

        RefleshCategories();

        BoothUrl = string.Format(BoothLink.ItemURLWithoutAuthorFormat, Localizer.Instance[Loc.BoothLanguageCode], launchInfo.BoothId);
        Title = string.Empty;
        Author = string.Empty;
        AuthorId = string.Empty;
        BoothId = string.Empty;
        Memo = string.Empty;
        SupportedAvatars = [];
        Tags = [];
        SelectedCategoryIndex = -1;
        SelectedCategoryIndex = 0;

        UpdateCountField();
        IsVisible = true;

        AddPaths(launchInfo.AssetPaths);
        await FetchBoothData();
    }

    public async void Open(BLMImportItemInfo launchInfo)
    {
        if (IsVisible && BoothId == launchInfo.ItemID)
        {
            ItemPaths.Add(new ItemPathViewModel(launchInfo.DownloadableFilename, launchInfo.DownloadURL, ItemPathType.URL));
            return;
        }

        Identifier = null;
        ItemPaths.Clear();

        RefleshCategories();

        BoothUrl = string.Format(BoothLink.ItemURLWithoutAuthorFormat, Localizer.Instance[Loc.BoothLanguageCode], launchInfo.ItemID);
        Title = string.Empty;
        Author = string.Empty;
        AuthorId = string.Empty;
        BoothId = string.Empty;
        Memo = string.Empty;
        SupportedAvatars = [];
        Tags = [];
        SelectedCategoryIndex = -1;
        SelectedCategoryIndex = 0;

        UpdateCountField();
        IsVisible = true;

        ItemPaths.Add(new ItemPathViewModel(launchInfo.DownloadableFilename, launchInfo.DownloadURL, ItemPathType.URL));
        await FetchBoothData();
    }

    public void AddPaths(string[] paths)
    {
        if (!IsVisible) Open();
        ItemPaths.AddRange(paths.Select(i =>
        {
            var itemPathType = ItemPathType.Unknown;
            if (i.StartsWith("http")) itemPathType = ItemPathType.URL;
            else if (File.Exists(i)) itemPathType = ItemPathType.File;
            else if (Directory.Exists(i)) itemPathType = ItemPathType.Folder;

            var fileName = itemPathType == ItemPathType.URL
                ? Path.GetFileName(new Uri(i).GetLeftPart(UriPartial.Path))
                : Path.GetFileName(i);

            return new ItemPathViewModel(fileName, i, itemPathType);
        }));
    }

    public void Close()
    {
        SelectedCategoryIndex = -1;
        IsVisible = false;
    }

    public int GetCategoryIndex(ItemCategory category)
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            if (Categories[i].Category.Equals(category))
            {
                return i;
            }
        }

        return -1;
    }

    private void Cancel()
    {
        Close();
    }

    private async Task Confirm()
    {
        var identifier = Identifier != null ? await ConfirmEdit(Identifier) : await ConfirmCreate();
        await AddPathsAsync(identifier);
    }

    private async Task<string> ConfirmEdit(string identifier)
    {
        var editContext = new ItemEditContext
        {
            Title = Title,
            Author = Author,
            AuthorId = AuthorId,
            BoothId = ValueParser.Int(BoothId, -1),
            ItemType = SelectedCategory?.Category.Type ?? ItemType.Avatar,
            CustomCategory = SelectedCategory?.Category.Type == ItemType.Custom ? SelectedCategory.Category.CustomCategory : string.Empty,
            ThumbnailUrl = ThumbnailUrl,
            SupportedAvatars = SupportedAvatars.ToList(),
            ItemMemo = Memo,
            Tags = Tags.ToList()
        };

        bool updateResult = await AvatarExplorerApp.Instance.Items.Update(identifier, editContext);
        MainWindowViewModel.Instance.ShowNotification(
            Localizer.Instance[updateResult ? Loc.Success.Default : Loc.Error.Default],
            Localizer.Instance[updateResult ? Loc.Success.ItemEdit : Loc.Error.ItemEditFailed],
            updateResult ? Avalonia.Controls.Notifications.NotificationType.Success : Avalonia.Controls.Notifications.NotificationType.Error
        );
        return identifier;
    }
    private async Task<string> ConfirmCreate()
    {
        var creationContext = new ItemCreationContext
        {
            Title = Title,
            Author = Author,
            AuthorId = AuthorId,
            BoothId = int.TryParse(BoothId, out var boothId) ? boothId : -1,
            ItemType = SelectedCategory?.Category.Type ?? ItemType.Avatar,
            CustomCategory = SelectedCategory?.Category.Type == ItemType.Custom ? SelectedCategory.Category.CustomCategory ?? string.Empty : string.Empty,
            ThumbnailUrl = ThumbnailUrl,
            SupportedAvatars = SupportedAvatars,
            ItemMemo = Memo,
            Tags = Tags
        };

        var existingSameBoothIdItem = AvatarExplorerApp.Instance.Items.GetAll().FirstOrDefault(i => i.BoothId == creationContext.BoothId);

        if (existingSameBoothIdItem != null)
        {
            var addToExistingItem = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Dialog.Confirmation.AddToExistingItem]
            );
            if (addToExistingItem) return existingSameBoothIdItem.Identifier;
        }

        var item = await AvatarExplorerApp.Instance.Items.Create(creationContext);
        MainWindowViewModel.Instance.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.ItemAdd],
            Avalonia.Controls.Notifications.NotificationType.Success
        );
        return item.Identifier;
    }

    private async Task AddPathsAsync(string identifier)
    {
        MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Default]);
        MainWindowViewModel.Instance.ProgressVM.Update(0);
        var result = await AvatarExplorerApp.Instance.Items.AddPaths(
            identifier,
            ItemPaths
                .Select(i => new ItemPathEntry()
                {
                    FileName = i.FileName,
                    Path = i.FullPath,
                    IsUrl = i.IsUrl
                }),
            ShouldLinkToOriginal
        );
        MainWindowViewModel.Instance.ProgressVM.Close();
        
        if (result.IsError)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.AddItemFileFailed],
                Avalonia.Controls.Notifications.NotificationType.Error
            );
        }
        else if (result.Value.ProcessingFailedPaths.Count > 0)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance.Get(Loc.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()),
                Avalonia.Controls.Notifications.NotificationType.Error
            );
        }

        AvatarExplorerApp.Instance.Items.Save();
        Close();
    }
    private async Task SelectAndAddFolders()
    {
        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: true
        );
        if (folders == null || folders.Length == 0) return;

        ItemPaths.AddRange(folders.Select(i => new ItemPathViewModel(Path.GetFileName(i), i, ItemPathType.Folder)));
    }
    private async Task SelectAndAddFiles()
    {
        var files = await StorageService.OpenFileDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFilePath],
            allowMultiple: true
        );
        if (files == null || files.Length == 0) return;

        ItemPaths.AddRange(files.Select(i => new ItemPathViewModel(Path.GetFileName(i), i, ItemPathType.File)));
    }
    private async Task AddUrl()
    {
        var url = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddUrl]);
        if (string.IsNullOrEmpty(url)) return;

        var fileName = Path.GetFileName(new Uri(url).GetLeftPart(UriPartial.Path));
        ItemPaths.Add(new ItemPathViewModel(fileName, url, ItemPathType.URL));
    }
    private async Task FetchBoothData()
    {
        MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Booth.Status.Fetching]);
        MainWindowViewModel.Instance.ProgressVM.Update(0);
        var fetchResult = await BoothService.Fetch(BoothUrl, waitCooldown: true);
        MainWindowViewModel.Instance.ProgressVM.Close();

        if (fetchResult.IsError)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.RetrieveBoothItemFailed],
                Avalonia.Controls.Notifications.NotificationType.Error
            );
            return;
        }

        var boothData = fetchResult.Value;
        Title = boothData.Title;
        Author = boothData.Shop.Name;
        SelectedCategoryIndex = GetCategoryIndex(boothData.EstimatedCategory);
        AuthorId = boothData.Shop.Id;
        BoothId = boothData.BoothId.ToString();
        ThumbnailUrl = boothData.ThumbnailUrl;
    }

    private void RemovePath(ItemPathViewModel pathModel) => ItemPaths.Remove(pathModel);

    private void RefleshCategories()
    {
        var itemGroupService = AvatarExplorerApp.Instance.ItemGroupService;
        var categories = itemGroupService.GetCategoryFolders(includeEmptyCategory: true)
            .Select(i => ResolveCategory(i.Identifier))
            .Where(i => i != null)
            .Cast<ItemCategory>();

        Categories.Clear();
        Categories.AddRange(categories.Select(i => new ItemCategoryViewModel(i).Update()));
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
        var newCategory = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.NewCustomCategoryName]);
        if (string.IsNullOrEmpty(newCategory)) return;

        Categories.Add(new ItemCategoryViewModel(new ItemCategory(newCategory)).Update());
    }

    private async Task SelectSupportedAvatars()
    {
        var avatars = await MainWindowViewModel.Instance.ShowSelectAvatars(
            Localizer.Instance[Loc.SelectAvatars.Title.SupportedAvatars],
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
        TagsText = Localizer.Instance.Get(Loc.ItemEditor.SelectedTagsCount, Tags.Count().ToString());
        SupportedAvatarsText = Localizer.Instance.Get(Loc.ItemEditor.SelectedAvatarsCount, GetSupportedAvatarsCount().ToString());
    }
}
