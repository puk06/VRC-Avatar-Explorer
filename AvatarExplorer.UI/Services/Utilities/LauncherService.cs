using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Services.System;
using ErrorOr;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class LauncherService
{
    private static ILauncher? GetLauncher(TopLevel? topLevel) => topLevel?.Launcher;

    internal static async Task<ErrorOr<Success>> OpenFile(TopLevel? topLevel, string filePath)
    {
        try
        {
            var launcher = GetLauncher(topLevel);
            if (launcher == null) return Error.Failure(description: "Failed to get launcher.");

            var fileInfo = new FileInfo(filePath);
            await launcher.LaunchFileInfoAsync(fileInfo);

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to open file: '{filePath}'.", ex);
            return Error.Failure(description: "Failed to open file.");
        }
    }

    internal static async Task<ErrorOr<Success>> OpenFolder(TopLevel? topLevel, string folderPath)
    {
        try
        {
            var launcher = GetLauncher(topLevel);
            if (launcher == null) return Error.Failure(description: "Failed to get launcher.");

            var folderInfo = new DirectoryInfo(folderPath);
            await launcher.LaunchDirectoryInfoAsync(folderInfo);
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError($"Failed to open directory: '{folderPath}'.", ex);
            return Error.Failure(description: "Failed to open directory.");
        }
    }

    internal static async Task<ErrorOr<Success>> OpenUri(TopLevel? topLevel, string uri)
    {
        try
        {
            var launcher = GetLauncher(topLevel);
            if (launcher == null) return Error.Failure(description: "Failed to get launcher.");

            var uriInfo = new Uri(uri);
            await launcher.LaunchUriAsync(uriInfo);

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError($"Failed to open Uri: '{uri}'.", ex);
            return Error.Failure(description: "Failed to open uri.");
        }
    }
}
