using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<string?>? _editMemoOverlay_tcs;

    private Task<string?> EditMemoOverlay_ShowAsync(string memo = "")
    {
        if (_editMemoOverlay_tcs != null) throw new InvalidOperationException("EditMemoOverlay is already shown.");

        _editMemoOverlay_tcs = new();

        EditMemoOverlay_MemoTextBox.Text = memo;
        EditMemoOverlay.IsVisible = true;

        return _editMemoOverlay_tcs.Task;
    }
    private async Task<string?> EditMemoOverlay_ShowSafeAsync(string memo = "")
    {
        try
        {
            return await EditMemoOverlay_ShowAsync(memo);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open Edit Memo dialog.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed]);
            return null;
        }
    }
    private void EditMemoOverlay_Close(string? result)
    {
        EditMemoOverlay.IsVisible = false;
        EditMemoOverlay_MemoTextBox.Text = string.Empty;

        TaskCompletionSource<string?>? tcs = _editMemoOverlay_tcs;
        _editMemoOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    #region Event Handler
    private void EditMemoOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditMemoOverlay_Close(null);
    private void EditMemoOverlay_Confirm_Click(object? sender, RoutedEventArgs e) => EditMemoOverlay_Close(EditMemoOverlay_MemoTextBox.Text ?? string.Empty);
    #endregion
}
