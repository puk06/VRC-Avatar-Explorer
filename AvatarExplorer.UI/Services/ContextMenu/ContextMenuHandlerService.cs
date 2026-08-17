using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Services.ContextMenu;

public static class ContextMenuHandlerService
{
    private static readonly Dictionary<ActionKey, Action<string>> _handlers = [];

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
        Register(ActionKey.RemoveFolder, RemoveFolder);
        Register(ActionKey.CheckForUpdate, CheckForUpdate);
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

    private static Item? GetByIdentifier(string identifier)
    {
        var item = InstanceRepository.Items.Get(identifier);
        if (item == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ItemNotFound],
                NotificationType.Error
            );
        }

        return item;
    }
    private static async Task EditItemInternal(string identifier, ItemEditContext context)
    {
        var result = await InstanceRepository.Items.Update(identifier, context);
        NotificationManager.Show(
            result ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            result ? Localizer.Instance[Loc.Success.ItemEdit] : Localizer.Instance[Loc.Error.ItemEditFailed],
            result ? NotificationType.Success : NotificationType.Error
        );
    }

    private static async void OpenFolder(string path)
    {
        var result = await LauncherService.OpenFolder(path);
        if (result.IsError)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.OpenFolderFailed],
                NotificationType.Error
            );
        }
    }
    private static async void RemoveFolder(string path)
    {
        var currentItem = InstanceRepository.NavigationService.GetCurrentItemId();
        var item = GetByIdentifier(currentItem ?? string.Empty);
        if (item == null) return;

        var isAppManaged = ItemUtils.IsAppManagedPath(item.ItemPath, path);
        if (isAppManaged)
        {
            var removeFromDatabase = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Localizer.Instance[Loc.Dialog.Confirmation.RemoveFolderFromApplicationManagedFolder], path)
            );
            if (!removeFromDatabase) return;
            
            await InstanceRepository.Items.RemovePath(item.Identifier, path, true);
        }
        else if (item.ItemPaths.Contains(path))
        {
            var removeFromDatabase = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveFolderFromDatabase, path)
            );
            if (!removeFromDatabase) return;

            var removeFolder = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveFolder, path)
            );

            await InstanceRepository.Items.RemovePath(item.Identifier, path, removeFolder);
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ItemPathNotFound],
                NotificationType.Error
            );
            return;
        }
        
        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.RemoveFolder],
            NotificationType.Success
        );
    }
    private static async void CheckForUpdate(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var updates = await InstanceRepository.VariationHashes.CheckVariationAndNotify(item.BoothId.ToString());
        if (updates.Count == 0)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.VariationUpdate.NoUpdatesAvailable],
                string.Empty,
                NotificationType.Information
            );
            return;
        }

        var contentLines = updates.Select(u =>
        {
            var variationName = string.IsNullOrEmpty(u.VariationName) ? Localizer.Instance[Loc.VariationUpdate.DefaultVariation] : u.VariationName;

            var parts = new List<string>();

            if (u.Diff.Added.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Added, u.Diff.Added.Count.ToString())}");
            if (u.Diff.Removed.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Removed, u.Diff.Removed.Count.ToString())}");
            if (u.Diff.Changed.Count > 0)
                parts.Add($"    {Localizer.Instance.Get(Loc.VariationUpdate.Changed, u.Diff.Changed.Count.ToString())}");

            return $"- {variationName}\n{string.Join("\n", parts)}";
        });

        NotificationManager.Show(
            Localizer.Instance[Loc.VariationUpdate.UpdateAvailable],
            string.Join("\n", contentLines),
            NotificationType.Information
        );
    }
    
    private static async void CopyBoothLink(string identifier)
    {
        var link = GetByIdentifier(identifier)?.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
        if (string.IsNullOrEmpty(link)) return;

        var result = await ClipboardService.SetText(link);
        NotificationManager.Show(
            !result.IsError ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            !result.IsError ? Localizer.Instance[Loc.Success.ClipboardSet] : Localizer.Instance[Loc.Error.ClipboardSetFailed],
            !result.IsError ? NotificationType.Success : NotificationType.Error
        );
    }
    private static async void OpenBoothLink(string identifier)
    {
        var link = GetByIdentifier(identifier)?.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]);
        if (string.IsNullOrEmpty(link)) return;

        var result = await LauncherService.OpenUri(link);
        if (result.IsError)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.OpenUriFailed],
                NotificationType.Error
            );
        }
    }
    private static void ShowOtherItemsByAuthor(string identifier)
    {
        var author = GetByIdentifier(identifier)?.Author;
        if (string.IsNullOrEmpty(author)) return;

        var mainVm = InstanceRepository.MainWindow.MainVM;
        mainVm.SearchText = $"Author=\"{author}\"";
    }
    private static async void ChangeThumbnail(string identifier)
    {
        var files = await StorageService.OpenFileDialog(Localizer.Instance[Loc.Dialog.SelectFilePath]);
        if (files == null || files.Length == 0) return;

        var result = await InstanceRepository.Items.UpdateThumbnail(identifier, files[0]);
        NotificationManager.Show(
            !result.IsError ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            !result.IsError ? Localizer.Instance[Loc.Success.ItemThumbnailEdit] : Localizer.Instance[Loc.Error.ItemThumbnailEditFailed],
            !result.IsError ? NotificationType.Success : NotificationType.Error
        );
    }
    private static async void FetchThumbnail(string identifier)
    {
        var result = await InstanceRepository.Items.FetchThumbnailFromBooth(identifier);
        NotificationManager.Show(
            !result.IsError ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            !result.IsError ? Localizer.Instance[Loc.Success.FetchItemThumbnail] : Localizer.Instance[Loc.Error.FetchItemThumbnailFailed],
            !result.IsError ? NotificationType.Success : NotificationType.Error
        );
    }
    private static async void CopyItemInfo(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        string itemInfo = string.Format("{0} - {1}\n{2}", item.Title, item.Author, item.BoothId != -1 ? item.GetBoothLink(Localizer.Instance[Loc.BoothLanguageCode]) : "(No Booth Link)");
        var result = await ClipboardService.SetText(itemInfo);
        NotificationManager.Show(
            !result.IsError ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            !result.IsError ? Localizer.Instance[Loc.Success.ClipboardSet] : Localizer.Instance[Loc.Error.ClipboardSetFailed],
            !result.IsError ? NotificationType.Success : NotificationType.Error
        );
    }
    private static void EditItem(string identifier) => InstanceRepository.MainWindow.ItemEditorVM.Open(identifier);
    private static async void EditItemTitle(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var newTitle = await InstanceRepository.MainWindow.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewItemTitle],
            item.Title
        );
        if (newTitle == null) return;

        await EditItemInternal(identifier, new() { Title = newTitle });
    }
    private static async void EditItemMemo(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var newMemo = await InstanceRepository.MainWindow.ShowEditMemoDialog(item.ItemMemo);
        if (newMemo == null) return;

        await EditItemInternal(identifier, new() { ItemMemo = newMemo });
    }
    private static void AddToBulkImportList(string identifier)
    {
        var bulkVm = InstanceRepository.MainWindow.MainVM.BulkImportVM;
        bulkVm.AddItem(identifier);
    }
    private static async void AddItemFile(string identifier)
    {
        var files = await StorageService.OpenFileDialog(
            Localizer.Instance[Loc.Dialog.SelectFilePath],
            allowMultiple: true
        );
        if (files == null || files.Length == 0) return;

        await AddPathsInternal(
            identifier,
            files.Select(i => new ItemPathEntry() { FileName = Path.GetFileName(i), Path = i }),
            isFile: true
        );
    }
    private static async void AddItemFolder(string identifier)
    {
        var folders = await StorageService.OpenFolderDialog(
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: true
        );
        if (folders == null || folders.Length == 0) return;

        await AddPathsInternal(
            identifier,
            folders.Select(i => new ItemPathEntry() { FileName = Path.GetFileName(i), Path = i }),
            isFile: false
        );
    }
    private static async Task AddPathsInternal(string identifier, IEnumerable<ItemPathEntry> paths, bool isFile)
    {
        var extractResult = await InstanceRepository.Items.AddPaths(identifier, paths, InstanceRepository.RuntimeSettings.Settings.ShouldLinkToOriginal);

        if (extractResult.IsError)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                isFile ? Localizer.Instance[Loc.Error.AddItemFileFailed] : Localizer.Instance[Loc.Error.AddItemFolderFailed],
                NotificationType.Error
            );
        }
        else if (extractResult.Value.ProcessingFailedPaths.Count > 0)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance.Get(
                    Loc.Error.FoundProcessingFailedPath,
                    extractResult.Value.ProcessingFailedPaths.Count.ToString()
                ),
                NotificationType.Warning
            );
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Success.Default],
                isFile ? Localizer.Instance[Loc.Success.ItemFileAdd] : Localizer.Instance[Loc.Success.ItemFolderAdd],
                NotificationType.Success
            );
        }
    }
    private static async void EditImplementedAvatar(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var newAvatars = await InstanceRepository.MainWindow.ShowSelectAvatars(
            Localizer.Instance[Loc.SelectAvatars.Title.ImplementedAvatars],
            item.ImplementedAvatars.ToArray(),
            includeCommonAvatar: false,
            includeTempAvatar: false,
            allowCreateTempAvatar: false
        );
        if (newAvatars == null) return;

        await EditItemInternal(identifier, new() { ImplementedAvatars = newAvatars });
    }
    private static async void EditItemDefaultPath(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var folders = await StorageService.OpenFolderDialog(
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: false
        );
        if (folders == null || folders.Length == 0) return;

        var result = await InstanceRepository.Items.Update(item.Identifier, new() { ItemPath = folders[0] });
        NotificationManager.Show(
            result ? Localizer.Instance[Loc.Success.Default] : Localizer.Instance[Loc.Error.Default],
            result ? Localizer.Instance[Loc.Success.ItemEdit] : Localizer.Instance[Loc.Error.ItemEditFailed],
            result ? NotificationType.Success : NotificationType.Error
        );
    }
    private static async void EditItemTag(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var newTags = await InstanceRepository.MainWindow.ShowEditTagsDialog(item.Tags.ToArray());
        if (newTags == null) return;

        await EditItemInternal(item.Identifier, new() { Tags = newTags });
    }
    private static async void RemoveItem(string identifier)
    {
        var item = GetByIdentifier(identifier);
        if (item == null) return;

        var removeResult = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveItem, item.Title)
        );
        if (!removeResult) return;

        bool removeDirectory = false;

        if (!string.IsNullOrEmpty(item.ItemPath) && Directory.Exists(item.ItemPath))
        {
            removeDirectory = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveAssetData, item.ItemPath)
            );
        }

        InstanceRepository.ItemGroupService.RemoveItem(item.Identifier, removeDirectory);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.Remove],
            NotificationType.Success
        );
    }
    private static async void OpenFile(string path)
    {
        var result = await LauncherService.OpenFile(path);
        if (result.IsError)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.OpenFileFailed],
                NotificationType.Error
            );
        }
    }
    private static void AddFileToBulkImportList(string path)
    {
        var currentItem = InstanceRepository.NavigationService.GetCurrentItemId();
        if (string.IsNullOrEmpty(currentItem)) return;
        
        var bulkVm = InstanceRepository.MainWindow.MainVM.BulkImportVM;
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
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.OpenFileFailed],
                NotificationType.Error
            );
            ErrorManager.Instance.PostError(string.Format("Failed to open file in explorer. '{0}'", path), ex);
        }
    }
    private static void OpenUnitypackageViewer(string path)
    {
        InstanceRepository.MainWindow.UnitypackageViewerVM.Open(path);
    }
    private static void OpenPdfViewer(string path)
    {
        InstanceRepository.MainWindow.PdfViewerVM.Open(path);
    }
    private static async void RemovePreset(string identifier)
    {
        var preset = InstanceRepository.BulkImportPresets.Get(identifier);
        if (preset == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.PresetNotFound],
                NotificationType.Error
            );
            return;
        }

        var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemovePreset, preset.PresetName)
        );
        if (!result) return;

        InstanceRepository.BulkImportPresets.Remove(preset.Identifier);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.Remove],
            NotificationType.Success
        );
    }
    private static async void EditTempAvatarName(string identifier)
    {
        var tempAvatar = InstanceRepository.TempAvatars.Get(identifier);
        if (tempAvatar == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var newName = await InstanceRepository.MainWindow.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewTempAvatarName],
            tempAvatar.AvatarName
        );
        if (string.IsNullOrEmpty(newName)) return;

        InstanceRepository.TempAvatars.RenameAvatar(tempAvatar.Identifier, newName);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.ItemEdit],
            NotificationType.Success
        );
    }
    private static void ResolveTempAvatar(string identifier)
    {
        InstanceRepository.MainWindow.ResolveTempAvatarVM.Open(identifier);
    }
    private static async void RemoveTempAvatar(string identifier)
    {
        var tempAvatar = InstanceRepository.TempAvatars.Get(identifier);
        if (tempAvatar == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.TempAvatarNotFound],
                NotificationType.Error
            );
            return;
        }

        var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveTempAvatar, tempAvatar.AvatarName)
        );
        if (!result) return;

        InstanceRepository.ItemGroupService.RemoveTempAvatar(tempAvatar.Identifier);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.Remove],
            NotificationType.Success
        );
    }
    private static async void EditCustomCategoryName(string identifier)
    {
        var oldCategory = ItemCategory.FromIdentifier(identifier).CustomCategory;

        var newName = await InstanceRepository.MainWindow.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewCustomCategoryName],
            oldCategory
        );
        if (string.IsNullOrEmpty(newName)) return;

        var isDuplicate = InstanceRepository.Items.GetAll()
            .Any(i => i.Category.Type == ItemType.Custom && i.Category.CustomCategory == newName);

        if (isDuplicate)
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Dialog.Confirmation.DuplicateCustomCategoryName]
            );
            if (!result) return;
        }

        InstanceRepository.Items.RenameCustomCategory(oldCategory, newName);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.RenameCustomCategory],
            NotificationType.Success
        );
    }
    private static void MergeWithOtherCategory(string identifier)
    {
        InstanceRepository.MainWindow.MergeCategoryVM.Open(identifier);
    }
}
