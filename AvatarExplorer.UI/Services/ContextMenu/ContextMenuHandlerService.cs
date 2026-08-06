using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Services.ContextMenu;

public static class ContextMenuHandlerService
{
    private static readonly Dictionary<ActionKey, Action<string>> _handlers = new();

    private static ItemRepository Items => AvatarExplorerApp.Instance.Items;
    private static ItemGroupService ItemGroupService => AvatarExplorerApp.Instance.ItemGroupService;
    private static ItemNavigationService ItemNavigationService => AvatarExplorerApp.Instance.ItemNavigationService;
    private static RuntimeSettings RuntimeSettings => AvatarExplorerApp.Instance.RuntimeSettings.Settings;

    public static void Register(ActionKey key, Action<string> handler)
    {
        _handlers[key] = handler;
    }

    // valueはActualValueがあればActualValue（ファイルパスなど）、無ければIdentidier
    public static void Handle(ActionKey key, string value)
    {
        if (_handlers.TryGetValue(key, out var handler))
        {
            handler(value);
        }
    }

    public static void Initialize()
    {
        Register(ActionKey.OpenFolder, OpenFolder);
        Register(ActionKey.CopyBoothLink, CopyBoothLink);
        Register(ActionKey.OpenBoothLink, OpenBoothLink);
        Register(ActionKey.ShowOtherItemsByAuthor, ShowOtherItemsByAuthor);
        Register(ActionKey.ChangeThumbnail, ChangeThumbnail);
        Register(ActionKey.FetchThumbnail, FetchThumbnail);
        Register(ActionKey.CopyItemInfo, CopyItemInfo);
        Register(ActionKey.EditItem, EditItem);
        Register(ActionKey.EditItemTitle, EditItemTitle);
        Register(ActionKey.EditItemMemo, EditItemMemo);
        Register(ActionKey.AddToBulkImportList, AddToBulkImportList);
        Register(ActionKey.AddItemFile, AddItemFile);
        Register(ActionKey.AddItemFolder, AddItemFolder);
        Register(ActionKey.EditImplementedAvatar, EditImplementedAvatar);
        Register(ActionKey.EditItemDefaultPath, EditItemDefaultPath);
        Register(ActionKey.EditItemTag, EditItemTag);
        Register(ActionKey.RemoveItem, RemoveItem);
        Register(ActionKey.OpenFile, OpenFile);
        Register(ActionKey.AddFileToBulkImportList, AddFileToBulkImportList);
        Register(ActionKey.ShowInExplorer, ShowInExplorer);
        Register(ActionKey.OpenUnitypackageViewer, OpenUnitypackageViewer);
        Register(ActionKey.OpenPdfViewer, OpenPdfViewer);
        Register(ActionKey.RemovePreset, RemovePreset);
        Register(ActionKey.EditTempAvatarName, EditTempAvatarName);
        Register(ActionKey.ResolveTempAvatar, ResolveTempAvatar);
        Register(ActionKey.RemoveTempAvatar, RemoveTempAvatar);
        Register(ActionKey.EditCustomCategoryName, EditCustomCategoryName);
        Register(ActionKey.MergeWithOtherCategory, MergeWithOtherCategory);
    }

    private static async void OpenFolder(string path)
    {
        await LauncherService.OpenFolder(TopLevelProvider.Current, path);
    }
    private static async void CopyBoothLink(string identifier)
    {
        var link = Items.Get(identifier)?.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
        if (string.IsNullOrEmpty(link)) return;

        await ClipboardService.SetText(link);
    }
    private static async void OpenBoothLink(string identifier)
    {
        var link = Items.Get(identifier)?.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
        if (string.IsNullOrEmpty(link)) return;

        await LauncherService.OpenUri(TopLevelProvider.Current, link);
    }
    private static void ShowOtherItemsByAuthor(string identifier)
    {
        var author = Items.Get(identifier)?.Author;
        if (string.IsNullOrEmpty(author)) return;

        var mainVm = MainWindowViewModel.Instance.MainVM;
        mainVm.SearchText = $"Author=\"{author}\"";
    }
    private static async void ChangeThumbnail(string identifier)
    {
        var files = await StorageService.OpenFileDialog(TopLevelProvider.Current, "Select Thumbnail Image");
        if (files == null || files.Length == 0) return;

        await Items.UpdateThumbnail(identifier, files[0]);
    }
    private static async void FetchThumbnail(string identifier)
    {
        await Items.FetchThumbnailFromBooth(identifier);
    }
    private static async void CopyItemInfo(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        string itemInfo = string.Format("{0} - {1}\n{2}", item.Title, item.Author, item.BoothId != -1 ? item.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]) : "(No Booth Link)");
        await ClipboardService.SetText(itemInfo);
    }
    private static void EditItem(string identifier)
    {
        MainWindowViewModel.Instance.ShowItemEditor(identifier);
    }
    private static async void EditItemTitle(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var newTitle = await MainWindowViewModel.Instance.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewItemTitle],
            item.Title
        );
        if (newTitle == null) return;

        await Items.Update(identifier, new() { Title = newTitle });
    }
    private static async void EditItemMemo(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var newMemo = await MainWindowViewModel.Instance.ShowEditMemoDialog(item.ItemMemo);
        if (newMemo == null) return;

        await Items.Update(identifier, new() { ItemMemo = newMemo });
    }
    private static void AddToBulkImportList(string identifier)
    {
        var bulkVm = MainWindowViewModel.Instance.MainVM.BulkImportVM;
        bulkVm.AddItem(identifier);
    }
    private static async void AddItemFile(string identifier)
    {
        var files = await StorageService.OpenFileDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFilePath],
            allowMultiple: true
        );
        if (files == null || files.Length == 0) return;

        await Items.AddPaths(identifier, files, RuntimeSettings.ShouldLinkToOriginal);
    }
    private static async void AddItemFolder(string identifier)
    {
        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: true
        );
        if (folders == null || folders.Length == 0) return;

        await Items.AddPaths(identifier, folders, RuntimeSettings.ShouldLinkToOriginal);
    }
    private static async void EditImplementedAvatar(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var newAvatars = await MainWindowViewModel.Instance.ShowSelectAvatars(
            Localizer.Instance[Loc.SelectAvatars.Title.ImplementedAvatars],
            item.ImplementedAvatars.ToArray(),
            includeCommonAvatar: false,
            includeTempAvatar: false,
            allowCreateTempAvatar: false
        );
        if (newAvatars == null) return;

        await Items.Update(identifier, new() { ImplementedAvatars = newAvatars });
    }
    private static async void EditItemDefaultPath(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: false
        );
        if (folders == null || folders.Length == 0) return;

        await Items.Update(item.Identifier, new() { ItemPath = folders[0] });
    }
    private static async void EditItemTag(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var newTags = await MainWindowViewModel.Instance.ShowEditTagsDialog(item.Tags.ToArray());
        if (newTags == null) return;

        await Items.Update(item.Identifier, new() { Tags = newTags });
    }
    private static async void RemoveItem(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var removeResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveItem, item.Title)
        );
        if (!removeResult) return;

        bool removeDirectory = false;

        if (!string.IsNullOrEmpty(item.ItemPath) && Directory.Exists(item.ItemPath))
        {
            removeDirectory = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveAssetData, item.ItemPath)
            );
        }

        ItemGroupService.RemoveItem(item.Identifier, removeDirectory);
    }
    private static async void OpenFile(string path)
    {
        var result = await LauncherService.OpenFile(TopLevelProvider.Current, path);
        if (result.IsError) {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.OpenFileFailed],
                Avalonia.Controls.Notifications.NotificationType.Error
            );
        }
    }
    private static void AddFileToBulkImportList(string path)
    {
        var currentItem = ItemNavigationService.GetCurrentItemId();
        if (string.IsNullOrEmpty(currentItem)) return;
        
        var bulkVm = MainWindowViewModel.Instance.MainVM.BulkImportVM;
        bulkVm.AddItem(currentItem, path);
    }
    private static void ShowInExplorer(string path)
    {
        if (!ProcessUtils.IsWindows()) return;

        try
        {
            Process.Start("explorer.exe", "/select," + path);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError(string.Format("Failed to open file in explorer. '{0}'", path), ex);
        }
    }
    private static void OpenUnitypackageViewer(string path)
    {
        MainWindowViewModel.Instance.ShowUnitypackageViewer(path);
    }
    private static void OpenPdfViewer(string path)
    {
        MainWindowViewModel.Instance.ShowPdfViewer(path);
    }
    private static async void RemovePreset(string identifier)
    {
        var preset = AvatarExplorerApp.Instance.BulkImportPresets.Get(identifier);
        if (preset == null) return;

        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemovePreset, preset.PresetName)
        );
        if (!result) return;

        AvatarExplorerApp.Instance.BulkImportPresets.Remove(preset.Identifier);

        MainWindowViewModel.Instance.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.Remove],
            Avalonia.Controls.Notifications.NotificationType.Success
        );
    }
    private static async void EditTempAvatarName(string identifier)
    {
        var tempAvatar = AvatarExplorerApp.Instance.TempAvatars.Get(identifier);
        if (tempAvatar == null) return;

        var newName = await MainWindowViewModel.Instance.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewTempAvatarName],
            tempAvatar.AvatarName
        );
        if (string.IsNullOrEmpty(newName)) return;

        AvatarExplorerApp.Instance.TempAvatars.RenameAvatar(tempAvatar.Identifier, newName);
    }
    private static void ResolveTempAvatar(string identifier)
    {
        MainWindowViewModel.Instance.ShowTempAvatarResolver(identifier);
    }
    private static async void RemoveTempAvatar(string identifier)
    {
        var tempAvatar = AvatarExplorerApp.Instance.TempAvatars.Get(identifier);
        if (tempAvatar == null) return;

        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveTempAvatar, tempAvatar.AvatarName)
        );
        if (!result) return;

        ItemGroupService.RemoveTempAvatar(tempAvatar.Identifier);
    }
    private static async void EditCustomCategoryName(string identifier)
    {
        // custom:ABC
        var oldCategory = identifier[(ItemNavigationService.CustomPrefix.Length + 1)..];

        var newName = await MainWindowViewModel.Instance.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewCustomCategoryName],
            oldCategory
        );
        if (string.IsNullOrEmpty(newName)) return;

        AvatarExplorerApp.Instance.Items.RenameCustomCategory(oldCategory, newName);
    }
    private static void MergeWithOtherCategory(string identifier)
    {
        MainWindowViewModel.Instance.MergeCategoryVM.Open(identifier);
    }
}
