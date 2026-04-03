using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.UI.Services.External;

internal static class UnitypackageService
{
    internal static async Task<ModifiedUnitypackagesResult> Import(Dictionary<string, string> itemPathCategoryDictionary, Func<string, int, Task>? onProgress = null)
    {
        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (onProgress != null) await onProgress(tuple.localizationKey, tuple.progress);
            });
        }

        return await AvatarExplorerApp.ModifyUnitypackageFilePaths(itemPathCategoryDictionary, progressAction);
    }

    internal static ImmutableArray<string> GetUnitypackagePaths(string itemPath)
    {
        if (!Directory.Exists(itemPath)) return [];

        List<string> unitypackageFilePaths = new();

        foreach (string filePath in FileSystemService.EnumerateFiles(itemPath))
        {
            if (!PathUtils.IsUnitypackageFile(filePath)) continue;
            unitypackageFilePaths.Add(filePath);
        }

        return unitypackageFilePaths.ToImmutableArray();
    }
}
