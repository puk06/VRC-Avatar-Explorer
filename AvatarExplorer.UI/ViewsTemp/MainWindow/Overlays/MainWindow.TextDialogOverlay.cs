using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<string?>? _textDialogOverlay_tcs;

    private Task<string?> TextDialogOverlay_ShowAsync(string title, string initialText = "")
    {
        if (_textDialogOverlay_tcs != null) throw new InvalidOperationException("TextDialog is already shown.");

        _textDialogOverlay_tcs = new();

        // TextDialogOverlay_Title.Text = title;
        // TextDialogOverlay_Content.Text = initialText;
        // TextDialogOverlay.IsVisible = true;

        return _textDialogOverlay_tcs.Task;
    }
    private async Task<string?> TextDialogOverlay_ShowSafeAsync(string title, string initialText = "")
    {
        try
        {
            return await TextDialogOverlay_ShowAsync(title, initialText);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open dialog.", ex);
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed], isError: true);
            return null;
        }
    }
    private void TextDialogOverlay_Close(string? result)
    {
        // TextDialogOverlay.IsVisible = false;
        // TextDialogOverlay_Content.Text = string.Empty;

        var tcs = _textDialogOverlay_tcs;
        _textDialogOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    #region Event Handler
    private void TextDialogOverlay_Confirm_Click(object? sender, RoutedEventArgs e) => TextDialogOverlay_Close(string.Empty);
    private void TextDialogOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => TextDialogOverlay_Close(null);
    #endregion
}
