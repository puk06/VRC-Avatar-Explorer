using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
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
        Register(ActionKey.OpenItemFolder, OpenItemFolder);
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
        Register(ActionKey.OpenFileInExplorer, OpenFileInExplorer);
        Register(ActionKey.OpenUnitypackageViewer, OpenUnitypackageViewer);
        Register(ActionKey.OpenPdfViewer, OpenPdfViewer);
        Register(ActionKey.RemovePreset, RemovePreset);
        Register(ActionKey.EditTempAvatarName, EditTempAvatarName);
        Register(ActionKey.ResolveTempAvatar, ResolveTempAvatar);
        Register(ActionKey.RemoveTempAvatar, RemoveTempAvatar);
        Register(ActionKey.EditCustomCategoryName, EditCustomCategoryName);
        Register(ActionKey.MergeWithOtherCategory, MergeWithOtherCategory);
    }

    private static async void OpenItemFolder(string identifier)
    {
        var path = Items.Get(identifier)?.ItemPath;
        if (string.IsNullOrEmpty(path)) return;

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

        Items.Update(identifier, new() { Title = newTitle });
    }
    private static async void EditItemMemo(string identifier)
    {
        var item = Items.Get(identifier);
        if (item == null) return;

        var newMemo = await MainWindowViewModel.Instance.ShowTextDialog(
            Localizer.Instance[Loc.Dialog.Title.NewItemTitle],
            item.Title
        );
        if (newMemo == null) return;

        Items.Update(identifier, new() { ItemMemo = newMemo });
    }
    private static void AddToBulkImportList(string identifier) { } // TODO: 作る
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

        Items.Update(identifier, new() { ImplementedAvatars = newAvatars.ToList() });
    }
    private static void EditItemDefaultPath(string identifier) { }
    private static void EditItemTag(string identifier) { }
    private static void RemoveItem(string identifier) { }
    private static void OpenFile(string identifier) { }
    private static void AddFileToBulkImportList(string identifier) { }
    private static void OpenFileInExplorer(string identifier) { }
    private static void OpenUnitypackageViewer(string path)
    {
        MainWindowViewModel.Instance.ShowUnitypackageViewer(path);
    }
    private static void OpenPdfViewer(string path)
    {
        MainWindowViewModel.Instance.ShowPdfViewer(path);
    }
    private static void RemovePreset(string identifier) { }
    private static void EditTempAvatarName(string identifier) { }
    private static void ResolveTempAvatar(string identifier) { }
    private static void RemoveTempAvatar(string identifier) { }
    private static void EditCustomCategoryName(string identifier) { }
    private static void MergeWithOtherCategory(string identifier)
    {
        MainWindowViewModel.Instance.MergeCategoryVM.Open(identifier);
    }
}
