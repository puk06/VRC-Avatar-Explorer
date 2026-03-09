using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

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

    internal static IReadOnlyList<string> GetUnitypackagePaths(string itemPath)
    {
        List<string> unitypackageFilePaths = new();
        if (!Directory.Exists(itemPath)) return unitypackageFilePaths;

        foreach (string filePath in FileSystemService.EnumerateFiles(itemPath))
        {
            bool isUnitypackage = Path.GetExtension(filePath).Equals(".unitypackage", StringComparison.CurrentCultureIgnoreCase);
            if (!isUnitypackage) continue;

            unitypackageFilePaths.Add(filePath);
        }

        return unitypackageFilePaths;
    }
}
