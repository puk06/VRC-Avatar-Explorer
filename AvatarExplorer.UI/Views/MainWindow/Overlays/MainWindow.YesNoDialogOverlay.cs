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
    private TaskCompletionSource<YesNoResult>? _yesNoDialogOverlay_tcs;

    private Task<YesNoResult> YesNoDialogOverlay_ShowAsync(string title, string content)
    {
        if (_yesNoDialogOverlay_tcs != null) throw new InvalidOperationException("YesNoDialog is already shown.");

        _yesNoDialogOverlay_tcs = new();

        YesNoDialogOverlay_Title.Text = title;
        YesNoDialogOverlay_Content.Text = content;
        YesNoDialogOverlay.IsVisible = true;

        return _yesNoDialogOverlay_tcs.Task;
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
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed], isError: true);
            return null;
        }
    }
    private void YesNoDialogOverlay_Close(YesNoResult result)
    {
        YesNoDialogOverlay.IsVisible = false;
        YesNoDialogOverlay_Title.Text = string.Empty;
        YesNoDialogOverlay_Content.Text = string.Empty;

        TaskCompletionSource<YesNoResult>? tcs = _yesNoDialogOverlay_tcs;
        _yesNoDialogOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    #region Event Handler
    private void YesNoDialogOverlay_Yes_Click(object? sender, RoutedEventArgs e) => YesNoDialogOverlay_Close(YesNoResult.Yes);
    private void YesNoDialogOverlay_No_Click(object? sender, RoutedEventArgs e) => YesNoDialogOverlay_Close(YesNoResult.No);
    #endregion
}
