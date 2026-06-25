using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<string?>? _archivePasswordDialogOverlay_tcs;

    private Task<string?> ArchivePasswordDialogOverlay_ShowAsync(string fileName, int currentAttempt = 1, int maxAttempts = 3)
    {
        if (_archivePasswordDialogOverlay_tcs != null) throw new InvalidOperationException("ArchivePasswordDialog is already shown.");

        _archivePasswordDialogOverlay_tcs = new();

        ArchivePasswordDialogOverlay_FileName.Text = $"{Localizer.Instance[LocalizationKey.ArchivePasswordDialog.FileName]}: {fileName}";
        ArchivePasswordDialogOverlay_AttemptInfo.Text = $"{Localizer.Instance[LocalizationKey.ArchivePasswordDialog.Attempts]}: {currentAttempt}/{maxAttempts}";
        ArchivePasswordDialogOverlay_PasswordBox.Text = string.Empty;
        ArchivePasswordDialogOverlay.IsVisible = true;

        return _archivePasswordDialogOverlay_tcs.Task;
    }

    private async Task<string?> ArchivePasswordDialogOverlay_ShowSafeAsync(string fileName, int currentAttempt = 1, int maxAttempts = 3)
    {
        try
        {
            return await ArchivePasswordDialogOverlay_ShowAsync(fileName, currentAttempt, maxAttempts);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open dialog.", ex);
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed], isError: true);
            return null;
        }
    }

    private void ArchivePasswordDialogOverlay_Close(string? result)
    {
        ArchivePasswordDialogOverlay.IsVisible = false;
        ArchivePasswordDialogOverlay_PasswordBox.Text = string.Empty;

        TaskCompletionSource<string?>? tcs = _archivePasswordDialogOverlay_tcs;
        _archivePasswordDialogOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    #region Event Handler
    private void ArchivePasswordDialogOverlay_Confirm_Click(object? sender, RoutedEventArgs e) => ArchivePasswordDialogOverlay_Close(ArchivePasswordDialogOverlay_PasswordBox.Text);
    private void ArchivePasswordDialogOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => ArchivePasswordDialogOverlay_Close(null);
    #endregion
}

