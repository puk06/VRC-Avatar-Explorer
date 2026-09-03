// This code is borrowed from Avalonia
// Github Code URL: https://github.com/AvaloniaUI/AvaloniaUI.QuickGuides/blob/main/ClipboardOps/ViewModels/MainWindowViewModel.cs

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using AvatarExplorer.Core.Services.System;
using ErrorOr;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class ClipboardService
{
    internal static async Task<ErrorOr<Success>> SetText(string text)
    {
        try
        {
            var lifetime = Application.Current?.ApplicationLifetime;
            if (lifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow?.Clipboard is not { } provider)
            {
                return Error.Failure(description: "Failed to get clipboard provider.");
            }

            await provider.SetTextAsync(text);

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to set text to clipboard.", ex);
            return Error.Failure(description: "Failed to set text to clipboard.");
        }
    }
}
