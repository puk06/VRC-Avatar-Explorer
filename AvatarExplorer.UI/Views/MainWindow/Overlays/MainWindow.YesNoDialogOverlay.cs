using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<YesNoResult>? _yesNoTcs;

    private Task<YesNoResult> YesNoDialogOverlay_ShowAsync(string title, string content)
    {
        if (_yesNoTcs != null) throw new InvalidOperationException("YesNoDialog is already shown.");

        _yesNoTcs = new();

        YesNoDialogTitle.Text = title;
        YesNoDialogContent.Text = content;
        YesNoDialogOverlay.IsVisible = true;

        return _yesNoTcs.Task;
    }

    private async Task<YesNoResult?> YesNoDialogOverlay_ShowSafeAsync(string title, string message)
    {
        try
        {
            return await YesNoDialogOverlay_ShowAsync(title, message);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open dialog.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed]);
            return null;
        }
    }

    private void CloseDialog(YesNoResult result)
    {
        YesNoDialogOverlay.IsVisible = false;

        TaskCompletionSource<YesNoResult>? tcs = _yesNoTcs;
        _yesNoTcs = null;

        tcs?.TrySetResult(result);
    }

    private void YesNoDialog_Yes_Click(object? sender, RoutedEventArgs e) => CloseDialog(YesNoResult.Yes);

    private void YesNoDialog_No_Click(object? sender, RoutedEventArgs e) => CloseDialog(YesNoResult.No);
}
