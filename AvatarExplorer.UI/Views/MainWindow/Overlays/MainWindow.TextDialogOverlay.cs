using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<string?>? _textTcs;

    private Task<string?> TextDialogOverlay_ShowAsync(string title, string initialText = "")
    {
        if (_textTcs != null) throw new InvalidOperationException("TextDialog is already shown.");

        _textTcs = new();

        TextDialogOverlay_Title.Text = title;
        if (!string.IsNullOrEmpty(initialText)) TextDialogOverlay_Content.Text = initialText;
        TextDialogOverlay.IsVisible = true;

        return _textTcs.Task;
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
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed]);
            return null;
        }
    }

    private void TextDialogOverlay_Close(string? result)
    {
        TextDialogOverlay.IsVisible = false;

        TaskCompletionSource<string?>? tcs = _textTcs;
        _textTcs = null;

        tcs?.TrySetResult(result);
    }

    private void TextDialogOverlay_Add_Click(object? sender, RoutedEventArgs e) => TextDialogOverlay_Close(TextDialogOverlay_Content.Text);

    private void TextDialogOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => TextDialogOverlay_Close(null);
}
