using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.UI.Services.External;

internal static class UnitypackageService
{
    internal static async Task<ModifiedUnitypackagesResult> Import(IReadOnlyList<UnitypackageImportEntry> entries, Func<string, int, Task>? onProgress = null)
    {
        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (onProgress != null)
                    await onProgress(tuple.localizationKey, tuple.progress);
            });
        }

        return await FileSystemService.ModifyUnitypackageFilePathsAsync(entries, progressAction);
    }

    internal static ImmutableArray<string> GetUnitypackagePaths(IEnumerable<string> itemPaths)
    {
        var unitypackageFilePaths = ImmutableArray.CreateBuilder<string>();
        
        foreach (var itemPath in itemPaths)
        {
            if (string.IsNullOrEmpty(itemPath)) continue;

            foreach (var filePath in FileSystemService.EnumerateFiles(itemPath).SortByFileName())
            {
                if (!PathUtils.IsUnitypackageFile(filePath)) continue;
                unitypackageFilePaths.Add(filePath);
            }
        }

        return unitypackageFilePaths.ToImmutable();
    }
}
