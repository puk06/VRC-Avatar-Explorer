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
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.System;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using ErrorOr;
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
            var item = InstanceRepository.Items.Get(itemId);
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

        ShouldLinkToOriginal = InstanceRepository.RuntimeSettings.ShouldLinkToOriginal;

        UpdateCountField();
        IsVisible = true;
    }
    public async Task Open(LaunchInfo launchInfo)
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

    public async Task Open(BLMImportItemInfo launchInfo)
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

        pathsToRemove.AddRange(ItemPaths.Where(i => !uniquePaths.Add(i.FullPath)));
        foreach (var path in pathsToRemove)
        {
            ItemPaths.Remove(path);
        }
    }

    private void Close()
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

    private void Cancel() => Close();

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

        if (itemPaths.Count == 0) return;
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

        bool updateResult = await InstanceRepository.Items.Update(identifier, editContext);
        NotificationManager.Show(
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
            BoothId = ValueParser.Int(BoothId, -1),
            ItemType = SelectedCategory?.Category.Type ?? ItemType.Avatar,
            CustomCategory = SelectedCategory?.Category.Type == ItemType.Custom ? SelectedCategory.Category.CustomCategory ?? string.Empty : string.Empty,
            ThumbnailUrl = ThumbnailUrl,
            SupportedAvatars = SupportedAvatars,
            ItemMemo = Memo,
            Tags = Tags
        };

        if (creationContext.BoothId != -1)
        {
            var existingSameBoothIdItem = InstanceRepository.Items.GetAll().FirstOrDefault(i => i.BoothId == creationContext.BoothId);

            if (existingSameBoothIdItem != null)
            {
                var addToExistingItem = await InstanceRepository.MainWindow.ShowYesNoDialog(
                    Localizer.Instance[Loc.Dialog.Confirmation.Default],
                    Localizer.Instance[Loc.Dialog.Confirmation.AddToExistingItem]
                );
                if (addToExistingItem) return existingSameBoothIdItem.Identifier;
            }
        }

        var item = await InstanceRepository.Items.Create(creationContext);
        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.ItemAdd],
            NotificationType.Success
        );
        return item.Identifier;
    }

    private static async Task AddPathsInBackground(string identifier, List<ItemPathEntry> itemPaths, bool shouldLinkToOriginal)
    {
        NotificationManager.Show(
            Localizer.Instance[Loc.Processing.Default],
            Localizer.Instance[Loc.Processing.AddContent],
            NotificationType.Information
        );

        try
        {
            var result = await InstanceRepository.Items.AddPaths(identifier, itemPaths, shouldLinkToOriginal);

            if (result.IsError)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.AddContentFailed],
                    NotificationType.Error
                );
            }
            else if (result.Value.ProcessingFailedPaths.Count > 0)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance.Get(Loc.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()),
                    NotificationType.Error
                );
            }
            else
            {
                NotificationManager.Show(
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
            Localizer.Instance[Loc.Dialog.SelectFilePath],
            allowMultiple: true
        );
        if (files == null || files.Length == 0) return;

        ItemPaths.AddRange(files.Select(i => new ItemPathViewModel(Path.GetFileName(i), i, ItemPathType.File)));
        RemoveDuplicatePaths();
    }
    private async Task AddUrl()
    {
        var url = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddUrl]);
        if (string.IsNullOrEmpty(url)) return;

        if (!UriUtils.TryParse(url, out var uri))
        {
            NotificationManager.Show(
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
        ErrorOr<BoothItem>? fetchResult = null;
        await NotificationManager.ShowWithProgress(
            Localizer.Instance[Loc.Processing.Booth.Title],
            async progress =>
            {
                progress.Report(Localizer.Instance[Loc.Processing.Booth.Status.Fetching], 0);
                fetchResult = await BoothService.Fetch(BoothUrl, waitCooldown: true);
                progress.Report(Localizer.Instance[Loc.Processing.Booth.Status.Fetching], 100);
                await Task.Delay(300); // To ensure the progress bar is visible for a short time
            }
        );

        if (fetchResult?.IsError is false && fetchResult.Value.Value is BoothItem boothData)
        {
            Title = boothData.Title;
            Author = boothData.Shop.Name;
            SelectedCategoryIndex = GetCategoryIndex(boothData.EstimatedCategory);
            AuthorId = boothData.Shop.Id;
            BoothId = boothData.BoothId.ToString();
            ThumbnailUrl = boothData.ThumbnailUrl;

            NotificationManager.Show(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance[Loc.Success.FetchBoothItemInfo],
                NotificationType.Success
            );
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.RetrieveBoothItemFailed],
                NotificationType.Error
            );
        }
    }

    private void RemovePath(ItemPathViewModel pathModel) => ItemPaths.Remove(pathModel);

    private void RefleshCategories()
    {
        var categories = InstanceRepository.ItemGroupService
            .GetCategoryFolders(includeEmptyCategory: true)
            .Select(i => ItemCategory.FromIdentifier(i.Identifier));

        Categories.Clear();
        Categories.AddRange(categories.Select(i => new ItemCategoryViewModel(i).Update()));
    }

    private async Task AddCustomCategory()
    {
        var newCategory = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.AddCustomCategory]);
        if (string.IsNullOrEmpty(newCategory)) return;

        Categories.Add(new ItemCategoryViewModel(new ItemCategory(newCategory)).Update());
        SelectedCategoryIndex = Categories.Count - 1;
    }

    private async Task SelectSupportedAvatars()
    {
        var avatars = await InstanceRepository.MainWindow.ShowSelectAvatars(
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
        return InstanceRepository.ItemGroupService.GetAllSupportedAvatarsIds(SupportedAvatars, false).Length;
    }

    private async Task EditItemMemo()
    {
        var newMemo = await InstanceRepository.MainWindow.ShowEditMemoDialog(Memo);
        if (newMemo == null) return;

        Memo = newMemo;
    }
    private async Task EditItemTags()
    {
        var newTags = await InstanceRepository.MainWindow.ShowEditTagsDialog(Tags.ToArray());
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
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.Validation.EmptyTitle],
                NotificationType.Error
            );
            return false;
        }

        // Author
        if (string.IsNullOrWhiteSpace(Author))
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.Validation.EmptyAuthor],
                NotificationType.Error
            );
            return false;
        }

        // Category
        if (SelectedCategory == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.InvalidCategory],
                NotificationType.Error
            );
            return false;
        }

        return true;
    }
}
