using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class StorageService
{
    private static IStorageProvider? GetStorageProvider(Visual visual) => TopLevel.GetTopLevel(visual)?.StorageProvider;

    internal static async Task<string[]?> OpenFileDialog(Visual visual, string title, bool allowMultiple = false)
    {
        try
        {
            IStorageProvider? storageProvider = GetStorageProvider(visual);
            if (storageProvider == null) return [];

            IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple
            });

            string[] filePaths = files
                .Select(i => i.TryGetLocalPath())
                .Where(i => !string.IsNullOrEmpty(i) && File.Exists(i))
                .ToArray()!;

            return filePaths.Length == 0 ? null : filePaths;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open file picker.", ex);
            return null;
        }
    }

    internal static async Task<string[]?> OpenFolderDialog(Visual visual, string title, bool allowMultiple = false, string? initialPath = null)
    {
        try
        {
            IStorageProvider? storageProvider = GetStorageProvider(visual);
            if (storageProvider == null) return [];

            FolderPickerOpenOptions folderPickerOpenOptions = new()
            {
                Title = title,
                AllowMultiple = allowMultiple
            };

            if (!string.IsNullOrEmpty(initialPath)) folderPickerOpenOptions.SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(initialPath);

            IReadOnlyList<IStorageFolder> folders = await storageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);

            string[] FolderPaths = folders
                .Select(i => i.TryGetLocalPath())
                .Where(i => !string.IsNullOrEmpty(i) && Directory.Exists(i))
                .ToArray()!;

            return FolderPaths.Length == 0 ? null : FolderPaths;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open folder picker.", ex);
            return null;
        }
    }

    internal static async Task<string?> SaveFileDialog(Visual visual, string title, string defaultExtension)
    {
        try
            {
            IStorageProvider? storageProvider = GetStorageProvider(visual);
            if (storageProvider == null) return null;

            IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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

    internal static async Task<IStorageFile?> GetStorageFileFromPath(Visual visual, string filePath)
    {
        try
        {
            IStorageProvider? storageProvider = GetStorageProvider(visual);
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
