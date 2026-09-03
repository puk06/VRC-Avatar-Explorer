using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class FetchAllThumbnailsViewModel : ViewModelBase
{
    [Reactive] public partial bool IsVisible { get; set; }
    [Reactive] public partial string Status { get; set; } = string.Empty;
    [Reactive] public partial string Count { get; set; } = string.Empty;
    [Reactive] public partial string Eta { get; set; } = string.Empty;
    [Reactive] public partial string CurrentItem { get; set; } = string.Empty;
    [Reactive] public partial int Progress { get; set; } = 0;
    [Reactive] public partial bool IsCancelable { get; set; } = false;
    [Reactive] public partial bool IsStartable { get; set; } = true;

    public IReactiveCommand StartCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public FetchAllThumbnailsViewModel()
    {
        StartCommand = ReactiveCommand.CreateFromTask(StartInternal);
        CancelCommand = ReactiveCommand.Create(Cancel);
        CloseCommand = ReactiveCommand.Create(Close);
    }

    public void Open()
    {
        ResetUi();
        IsVisible = true;
    }

    private void Close()
    {
        if (_isRunning) return;
        IsVisible = false;
    }

    private void ResetUi()
    {
        Progress = 0;
        Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Ready];
        Count = Localizer.Instance.Get(Loc.FetchAllThumbnails.Progress, ["0", "0", "0", "0"]);
        CurrentItem = Localizer.Instance.Get(Loc.FetchAllThumbnails.CurrentItem, "-");
        Eta = Localizer.Instance[Loc.FetchAllThumbnails.EtaUnknown];

        IsStartable = !_isRunning;
        IsCancelable = _isRunning;
    }

    private static string FormatRemainingTime(int totalSeconds)
    {
        int clamped = Math.Max(0, totalSeconds);
        int minutes = clamped / 60;
        int seconds = clamped % 60;
        return Localizer.Instance.Get(Loc.FetchAllThumbnails.Eta, [minutes.ToString(), seconds.ToString()]);
    }

    private async Task StartInternal()
    {
        if (_isRunning) return;

        var allItems = InstanceRepository.Items.GetAll()
            .Where(i => i.BoothId != -1)
            .ToArray();

        if (allItems.Length == 0)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ItemNotFound],
                NotificationType.Error
            );
            return;
        }

        _isRunning = true;
        _cancellationTokenSource = new();
        var cancellationToken = _cancellationTokenSource.Token;

        IsStartable = false;
        IsCancelable = true;
        Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Running];

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

                Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Running];
                CurrentItem = Localizer.Instance.Get(Loc.FetchAllThumbnails.CurrentItem, item.Title);

                var result = await InstanceRepository.Items.FetchThumbnailFromBooth(item.Identifier);
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
                Progress = progress;
                Count = Localizer.Instance.Get(
                    Loc.FetchAllThumbnails.Progress,
                    [processedCount.ToString(), allItems.Length.ToString(), successCount.ToString(), failureCount.ToString()]
                );

                int remainingCount = allItems.Length - processedCount;
                if (processedCount > 0 && remainingCount > 0)
                {
                    var averageSecondsPerItem = (DateTime.UtcNow - startedAt).TotalSeconds / processedCount;
                    int estimatedRemainingSeconds = (int)Math.Round(averageSecondsPerItem * remainingCount, MidpointRounding.AwayFromZero);
                    Eta = FormatRemainingTime(estimatedRemainingSeconds);
                }
                else if (remainingCount == 0)
                {
                    Eta = FormatRemainingTime(0);
                }
                else
                {
                    Eta = Localizer.Instance[Loc.FetchAllThumbnails.EtaUnknown];
                }
            }
        }
        catch (TaskCanceledException)
        {
            isCancelled = true;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _isRunning = false;

            IsStartable = true;
            IsCancelable = false;
        }

        if (isCancelled)
        {
            Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Cancelled];
            NotificationManager.Show(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance.Get(Loc.Warning.FetchAllItemThumbnailsCancelled, [successCount.ToString(), failureCount.ToString(), allItems.Length.ToString()]),
                NotificationType.Warning
            );
            return;
        }

        Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Completed];

        if (failureCount == 0)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance.Get(Loc.Success.FetchAllItemThumbnails, successCount.ToString()),
                NotificationType.Success
            );
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance.Get(Loc.Error.FetchAllItemThumbnailsFailed, [successCount.ToString(), failureCount.ToString(), allItems.Length.ToString()]),
                NotificationType.Error
            );
        }
    }

    private void Cancel()
    {
        if (!_isRunning) return;

        _cancellationTokenSource?.Cancel();
        IsCancelable = false;
        Status = Localizer.Instance[Loc.FetchAllThumbnails.Status.Cancelling];
    }
}
