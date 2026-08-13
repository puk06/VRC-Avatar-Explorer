using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
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
    public ItemCategoryViewModel? SelectedCategory => (SelectedCategoryIndex > 0 && SelectedCategoryIndex < Categories.Count) ? Categories[SelectedCategoryIndex] : null;
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

    private void ResetFields()
    {
        Identifier = null;
        ItemPaths.Clear();
        BoothUrl = string.Empty;
        Title = string.Empty;
        Author = string.Empty;
        SelectedCategoryIndex = -1;
        AuthorId = string.Empty;
        BoothId = string.Empty;
        ThumbnailUrl = string.Empty;
        Memo = string.Empty;
        SupportedAvatars = [];
        Tags = [];
    }

    public void Open(string? itemId = null)
    {
        ResetFields();
        Identifier = itemId;
        RefleshCategories();

        if (itemId != null)
        {
            var item = AvatarExplorerApp.Instance.Items.Get(itemId);
            if (item != null)
            {
                if (item.BoothId != -1) BoothUrl = item.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
                Title = item.Title;
                Author = item.Author;
                AuthorId = item.AuthorId;
                if (item.BoothId != -1) BoothId = item.BoothId.ToString();
                Memo = item.ItemMemo;
                SupportedAvatars = item.SupportedAvatars;
                Tags = item.Tags;
                SelectedCategoryIndex = GetCategoryIndex(item.Category);
            }
        }
        else
        {
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

        ResetFields();
        BoothUrl = string.Format(BoothLink.ItemURLWithoutAuthorFormat, Localizer.Instance[Loc.BoothLanguageCode], launchInfo.BoothId);
        BoothId = launchInfo.BoothId;

        RefleshCategories();
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
            RemoveDuplicatePaths();
            return;
        }

        ResetFields();
        BoothUrl = string.Format(BoothLink.ItemURLWithoutAuthorFormat, Localizer.Instance[Loc.BoothLanguageCode], launchInfo.ItemID);
        BoothId = launchInfo.ItemID;

        RefleshCategories();
        SelectedCategoryIndex = 0;
        UpdateCountField();
        IsVisible = true;

        ItemPaths.Add(new ItemPathViewModel(launchInfo.DownloadableFilename, launchInfo.DownloadURL, ItemPathType.URL));
        RemoveDuplicatePaths();

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

            var fileName = itemPathType == ItemPathType.URL && UriUtils.TryParse(i, out var uri)
                ? Path.GetFileName(uri.GetLeftPart(UriPartial.Path))
                : Path.GetFileName(i);

            return new ItemPathViewModel(fileName, i, itemPathType);
        }));

        RemoveDuplicatePaths();
    }

    private void RemoveDuplicatePaths()
    {
        var uniquePaths = new HashSet<string>();
        var pathsToRemove = new List<ItemPathViewModel>();

        foreach (var path in ItemPaths)
        {
            if (!uniquePaths.Add(path.FullPath))
            {
                pathsToRemove.Add(path);
            }
        }

        foreach (var path in pathsToRemove)
        {
            ItemPaths.Remove(path);
        }
    }

    public void Close()
    {
        ResetFields();
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

        return 0; // Default = Avatar
    }

    private void Cancel()
    {
        Close();
    }

    private async Task Confirm()
    {
        var validationResult = ValidateFields();
        if (!validationResult) return;

        var identifier = Identifier != null ? await ConfirmEdit(Identifier) : await ConfirmCreate();

        var itemPaths = ItemPaths.Select(i => new ItemPathEntry
        {
            FileName = i.FileName,
            Path = i.FullPath,
            IsUrl = i.IsUrl
        }).ToList();
        var shouldLinkToOriginal = ShouldLinkToOriginal;

        Close();

        _ = AddPathsInBackground(identifier, itemPaths, shouldLinkToOriginal);
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
            SupportedAvatars = SupportedAvatars,
            ItemMemo = Memo,
            Tags = Tags
        };

        bool updateResult = await AvatarExplorerApp.Instance.Items.Update(identifier, editContext);
        MainWindowViewModel.ShowNotification(
            Localizer.Instance[updateResult ? Loc.Success.Default : Loc.Error.Default],
            Localizer.Instance[updateResult ? Loc.Success.ItemEdit : Loc.Error.ItemEditFailed],
            updateResult ? NotificationType.Success : NotificationType.Error
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

        if (creationContext.BoothId != -1)
        {
            var existingSameBoothIdItem = AvatarExplorerApp.Instance.Items.GetAll().FirstOrDefault(i => i.BoothId == creationContext.BoothId);

            if (existingSameBoothIdItem != null)
            {
                var addToExistingItem = await MainWindowViewModel.Instance.ShowYesNoDialog(
                    Localizer.Instance[Loc.Dialog.Confirmation.Default],
                    Localizer.Instance[Loc.Dialog.Confirmation.AddToExistingItem]
                );
                if (addToExistingItem) return existingSameBoothIdItem.Identifier;
            }
        }

        var item = await AvatarExplorerApp.Instance.Items.Create(creationContext);
        MainWindowViewModel.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.ItemAdd],
            NotificationType.Success
        );
        return item.Identifier;
    }

    private static async Task AddPathsInBackground(string identifier, List<ItemPathEntry> itemPaths, bool shouldLinkToOriginal)
    {
        MainWindowViewModel.ShowNotification(
            Localizer.Instance[Loc.Processing.Default],
            Localizer.Instance[Loc.Processing.AddContent],
            NotificationType.Information
        );

        try
        {
            var result = await AvatarExplorerApp.Instance.Items.AddPaths(identifier, itemPaths, shouldLinkToOriginal);

            if (result.IsError)
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.AddContentFailed],
                    NotificationType.Error
                );
            }
            else if (result.Value.ProcessingFailedPaths.Count > 0)
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance.Get(Loc.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()),
                    NotificationType.Error
                );
            }
            else
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Success.Default],
                    Localizer.Instance[Loc.Success.ContentAdd],
                    NotificationType.Success
                );
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to add item paths in background.", ex);
        }
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
        RemoveDuplicatePaths();
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
        RemoveDuplicatePaths();
    }
    private async Task AddUrl()
    {
        var url = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddUrl]);
        if (string.IsNullOrEmpty(url)) return;

        if (!UriUtils.TryParse(url, out var uri))
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.InvalidUrl],
                NotificationType.Error
            );
            return;
        }

        var fileName = Path.GetFileName(uri.GetLeftPart(UriPartial.Path));
        ItemPaths.Add(new ItemPathViewModel(fileName, url, ItemPathType.URL));
        RemoveDuplicatePaths();
    }
    private async Task FetchBoothData()
    {
        MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Booth.Status.Fetching]);
        MainWindowViewModel.Instance.ProgressVM.Update(0);
        var fetchResult = await BoothService.Fetch(BoothUrl, waitCooldown: true);
        MainWindowViewModel.Instance.ProgressVM.Close();

        if (fetchResult.IsError)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.RetrieveBoothItemFailed],
                NotificationType.Error
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
        var newCategory = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCustomCategory]);
        if (string.IsNullOrEmpty(newCategory)) return;

        Categories.Add(new ItemCategoryViewModel(new ItemCategory(newCategory)).Update());
        SelectedCategoryIndex = Categories.Count - 1;
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
        return itemGroup.GetAllSupportedAvatarsIds(SupportedAvatars, false).Length;
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

    private bool ValidateFields()
    {
        // Title
        if (string.IsNullOrWhiteSpace(Title))
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.Validation.EmptyTitle],
                NotificationType.Error
            );
            return false;
        }
        
        // Author
        if (string.IsNullOrWhiteSpace(Author))
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.Validation.EmptyAuthor],
                NotificationType.Error
            );
            return false;
        }

        // Category
        if (SelectedCategory == null)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.InvalidCategory],
                NotificationType.Error
            );
            return false;
        }

        // Supported Avatars
        if (SelectedCategory.Category.Type != ItemType.Clothing && SupportedAvatars.Any(i => i.StartsWith("commonavatar")))
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.Validation.NotClothingWithCommonAvatar],
                NotificationType.Error
            );
            return false;
        }

        return true;
    }
}
