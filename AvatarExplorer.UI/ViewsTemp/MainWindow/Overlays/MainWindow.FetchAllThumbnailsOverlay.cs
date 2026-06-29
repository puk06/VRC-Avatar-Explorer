using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private bool _fetchAllThumbnailsOverlay_isRunning = false;
    private CancellationTokenSource? _fetchAllThumbnailsOverlay_cancellationTokenSource;

    private void FetchAllThumbnailsOverlay_Show()
    {
        FetchAllThumbnailsOverlay_ResetUi();
        // FetchAllThumbnailsOverlay.IsVisible = true;
    }
    private void FetchAllThumbnailsOverlay_Hide()
    {
        if (_fetchAllThumbnailsOverlay_isRunning) return;
        // FetchAllThumbnailsOverlay.IsVisible = false;
    }

    private void FetchAllThumbnailsOverlay_ResetUi()
    {
        // FetchAllThumbnailsOverlay_ProgressBar.Value = 0;
        // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Ready"];
        // FetchAllThumbnailsOverlay_CountText.Text = Localizer.Instance.Get("FetchAllThumbnailsOverlay.Progress", ["0", "0", "0", "0"]);
        // FetchAllThumbnailsOverlay_CurrentItemText.Text = Localizer.Instance.Get("FetchAllThumbnailsOverlay.CurrentItem", "-");
        // FetchAllThumbnailsOverlay_EtaText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.EtaUnknown"];

        // FetchAllThumbnailsOverlay_StartButton.IsEnabled = !_fetchAllThumbnailsOverlay_isRunning;
        // FetchAllThumbnailsOverlay_CancelButton.IsEnabled = _fetchAllThumbnailsOverlay_isRunning;
        // FetchAllThumbnailsOverlay_CloseButton.IsEnabled = !_fetchAllThumbnailsOverlay_isRunning;
    }

    private static string FormatRemainingTime(int totalSeconds)
    {
        int clamped = Math.Max(0, totalSeconds);
        int minutes = clamped / 60;
        int seconds = clamped % 60;
        return Localizer.Instance.Get("FetchAllThumbnailsOverlay.Eta", [minutes.ToString(), seconds.ToString()]);
    }

    private async Task FetchAllThumbnailsOverlay_StartInternal()
    {
        if (_fetchAllThumbnailsOverlay_isRunning) return;

        var allItems = AvatarExplorer.GetAllItems();
        if (allItems.Length == 0)
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.Nothing], isError: true);
            return;
        }

        _fetchAllThumbnailsOverlay_isRunning = true;
        _fetchAllThumbnailsOverlay_cancellationTokenSource = new();
        var cancellationToken = _fetchAllThumbnailsOverlay_cancellationTokenSource.Token;

        // FetchAllThumbnailsOverlay_StartButton.IsEnabled = false;
        // FetchAllThumbnailsOverlay_CancelButton.IsEnabled = true;
        // FetchAllThumbnailsOverlay_CloseButton.IsEnabled = false;
        // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Running"];

        int successCount = 0;
        int failureCount = 0;
        var isCancelled = false;
        var startedAt = DateTime.UtcNow;

        try
        {
            for (int index = 0; index < allItems.Length; index++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    isCancelled = true;
                    break;
                }

                var item = allItems[index];

                // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Running"];
                // FetchAllThumbnailsOverlay_CurrentItemText.Text = Localizer.Instance.Get("FetchAllThumbnailsOverlay.CurrentItem", item.Title);

                await AvatarExplorer.WaitForApiCooldownAsync(cancellationToken: cancellationToken);

                var result = await AvatarExplorer.FetchAndUpdateThumbnailImage(item.Id);
                if (result.IsError)
                {
                    failureCount++;
                    ErrorManager.Instance.PostInternalError($"Failed to fetch item thumbnail in bulk process. ItemId: '{item.Id}'.", tag: result.Errors.ToErrorString());
                }
                else
                {
                    successCount++;
                }

                int processedCount = index + 1;
                int progress = (int)Math.Clamp(processedCount * 100.0 / allItems.Length, 0, 100);
                // FetchAllThumbnailsOverlay_ProgressBar.Value = progress;
                // FetchAllThumbnailsOverlay_CountText.Text = Localizer.Instance.Get(
                //     "FetchAllThumbnailsOverlay.Progress",
                //     [processedCount.ToString(), allItems.Length.ToString(), successCount.ToString(), failureCount.ToString()]
                // );

                int remainingCount = allItems.Length - processedCount;
                if (processedCount > 0 && remainingCount > 0)
                {
                    var averageSecondsPerItem = (DateTime.UtcNow - startedAt).TotalSeconds / processedCount;
                    int estimatedRemainingSeconds = (int)Math.Round(averageSecondsPerItem * remainingCount, MidpointRounding.AwayFromZero);
                    // FetchAllThumbnailsOverlay_EtaText.Text = FormatRemainingTime(estimatedRemainingSeconds);
                }
                else if (remainingCount == 0)
                {
                    // FetchAllThumbnailsOverlay_EtaText.Text = FormatRemainingTime(0);
                }
                else
                {
                    // FetchAllThumbnailsOverlay_EtaText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.EtaUnknown"];
                }
            }
        }
        catch (TaskCanceledException)
        {
            isCancelled = true;
        }
        finally
        {
            _fetchAllThumbnailsOverlay_cancellationTokenSource?.Dispose();
            _fetchAllThumbnailsOverlay_cancellationTokenSource = null;
            _fetchAllThumbnailsOverlay_isRunning = false;

            // FetchAllThumbnailsOverlay_StartButton.IsEnabled = true;
            // FetchAllThumbnailsOverlay_CancelButton.IsEnabled = false;
            // FetchAllThumbnailsOverlay_CloseButton.IsEnabled = true;
        }

        if (successCount > 0)
        {
            Main_ReloadCurrentWindow();
        }

        if (isCancelled)
        {
            // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Cancelled"];
            Main_ShowNotification(
                Localizer.Instance[LocalizationKey.Warning.Default],
                Localizer.Instance.Get(LocalizationKey.Warning.FetchAllItemThumbnailsCancelled, [successCount.ToString(), failureCount.ToString(), allItems.Length.ToString()])
            );
            return;
        }

        // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Completed"];

        if (failureCount == 0)
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance.Get("Success.FetchAllItemThumbnails", successCount.ToString()), isSuccess: true);
        }
        else
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get("Error.FetchAllItemThumbnailsFailed", [successCount.ToString(), failureCount.ToString(), allItems.Length.ToString()]), isError: true);
        }
    }

    #region Event Handler
    private void FetchAllThumbnailsOverlay_Close_Click(object? sender, RoutedEventArgs e) => FetchAllThumbnailsOverlay_Hide();
    private async void FetchAllThumbnailsOverlay_Start_Click(object? sender, RoutedEventArgs e) => await FetchAllThumbnailsOverlay_StartInternal();
    private void FetchAllThumbnailsOverlay_Cancel_Click(object? sender, RoutedEventArgs e)
    {
        if (!_fetchAllThumbnailsOverlay_isRunning) return;

        _fetchAllThumbnailsOverlay_cancellationTokenSource?.Cancel();
        // FetchAllThumbnailsOverlay_CancelButton.IsEnabled = false;
        // FetchAllThumbnailsOverlay_StatusText.Text = Localizer.Instance["FetchAllThumbnailsOverlay.Status.Cancelling"];
    }
    #endregion
}
