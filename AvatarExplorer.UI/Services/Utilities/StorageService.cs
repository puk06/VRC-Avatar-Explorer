using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Services.System;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class StorageService
{
    private static IStorageProvider? GetStorageProvider(TopLevel? topLebel) => topLebel?.StorageProvider;

    internal static async Task<string[]?> OpenFileDialog(string title, bool allowMultiple = false)
    {
        try
        {
            var storageProvider = GetStorageProvider(TopLevelProvider.Current);
            if (storageProvider == null) return [];

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple
            });

            var filePaths = files
                .Select(i => i.TryGetLocalPath())
                .Where(i => !string.IsNullOrEmpty(i) && File.Exists(i))
                .Cast<string>()
                .ToArray();

            return filePaths.Length == 0 ? null : filePaths;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open file picker.", ex);
            return null;
        }
    }

    internal static async Task<string[]?> OpenFolderDialog(string title, bool allowMultiple = false, string? initialPath = null)
    {
        try
        {
            var storageProvider = GetStorageProvider(TopLevelProvider.Current);
            if (storageProvider == null) return [];

            var folderPickerOpenOptions = new FolderPickerOpenOptions()
            {
                Title = title,
                AllowMultiple = allowMultiple
            };

            if (!string.IsNullOrEmpty(initialPath)) folderPickerOpenOptions.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(initialPath);

            var folders = await storageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);

            var FolderPaths = folders
                .Select(i => i.TryGetLocalPath())
                .Where(i => !string.IsNullOrEmpty(i) && Directory.Exists(i))
                .Cast<string>()
                .ToArray();

            return FolderPaths.Length == 0 ? null : FolderPaths;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open folder picker.", ex);
            return null;
        }
    }

    internal static async Task<string?> OpenSaveFileDialog(string title, string defaultExtension)
    {
        try
        {
            var storageProvider = GetStorageProvider(TopLevelProvider.Current);
            if (storageProvider == null) return null;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = defaultExtension
            });

            return file?.TryGetLocalPath();
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to safe file picker.", ex);
            return null;
        }
    }

    internal static async Task<IStorageFile?> GetStorageFileFromPath(string filePath)
    {
        try
        {
            var storageProvider = GetStorageProvider(TopLevelProvider.Current);
            if (storageProvider == null) return null;

            return await storageProvider.TryGetFileFromPathAsync(filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to get storage file from path: '{filePath}'.", ex);
            return null;
        }
    }
}
