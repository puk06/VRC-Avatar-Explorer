using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;

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

        var result = await FileSystemService.ModifyUnitypackageFilePathsAsync(entries, progressAction);
        return result;
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

    public static string GetCategoryDisplayName(ItemCategory category)
    {
        return category.IsLocalizable
            ? Localizer.Instance[category.ToString()]
            : category.ToString();
    }

    public static async Task ImportWithProgress(
        IReadOnlyList<UnitypackageImportEntry> entries,
        string errorKey = Loc.Error.ImportUnitypackageFailed)
    {
        ModifiedUnitypackagesResult? importResult = null;

        await NotificationManager.ShowWithProgress(
            Localizer.Instance[Loc.Processing.Unitypackage.Title],
            async progress =>
            {
                importResult = await Import(
                    entries,
                    onProgress: (name, percent) =>
                    {
                        progress.Report(Localizer.Instance.Get(name, percent.ToString()), percent);
                        return Task.CompletedTask;
                    }
                );
            }
        );

        if (importResult == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[errorKey],
                NotificationType.Error
            );
            return;
        }

        HandleImportResult(importResult, errorKey);
    }

    public static void HandleImportResult(ModifiedUnitypackagesResult importResult, string errorKey = Loc.Error.ImportUnitypackageFailed)
    {
        if (importResult.ContainsScripts)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance[Loc.Warning.ScriptsFoundInUnitypackage],
                NotificationType.Warning
            );
        }

        if (string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[errorKey],
                NotificationType.Error
            );
            return;
        }

        _ = OpenModifiedUnitypackage(importResult.ModifiedUnitypackagePath);
    }

    public static async Task OpenModifiedUnitypackage(string path)
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
}
