using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class FetchAllVariationHashsViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public string Status { get; set; } = string.Empty;
    [Reactive] public string Count { get; set; } = string.Empty;
    [Reactive] public string Eta { get; set; } = string.Empty;
    [Reactive] public string CurrentItem { get; set; } = string.Empty;
    [Reactive] public int Progress { get; set; } = 0;
    [Reactive] public bool IsCancelable { get; set; } = false;
    [Reactive] public bool IsStartable { get; set; } = true;

    public IReactiveCommand StartCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    private bool _isRunning = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public FetchAllVariationHashsViewModel()
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
        Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Ready];
        Count = Localizer.Instance.Get(Loc.FetchAllVariationHashs.Progress, ["0", "0", "0", "0"]);
        CurrentItem = Localizer.Instance.Get(Loc.FetchAllVariationHashs.CurrentItem, "-");
        Eta = Localizer.Instance[Loc.FetchAllVariationHashs.EtaUnknown];

        IsStartable = !_isRunning;
        IsCancelable = _isRunning;
    }

    private static string FormatRemainingTime(int totalSeconds)
    {
        int clamped = Math.Max(0, totalSeconds);
        int minutes = clamped / 60;
        int seconds = clamped % 60;
        return Localizer.Instance.Get(Loc.FetchAllVariationHashs.Eta, [minutes.ToString(), seconds.ToString()]);
    }

    private async Task StartInternal()
    {
        if (_isRunning) return;

        var allItems = AvatarExplorerApp.Instance.Items.GetAll()
            .Where(i => i.BoothId != -1)
            .ToArray();

        if (allItems.Length == 0)
        {
            MainWindowViewModel.ShowNotification(
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
        Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Running];

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

                Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Running];
                CurrentItem = Localizer.Instance.Get(Loc.FetchAllVariationHashs.CurrentItem, item.Title);

                var result = await AvatarExplorerApp.Instance.VariationHashes.EnsureVariationHash(item.BoothId.ToString());
                if (!result) failureCount++;
                else successCount++;

                int processedCount = index + 1;
                int progress = (int)Math.Clamp(processedCount * 100.0 / allItems.Length, 0, 100);
                Progress = progress;
                Count = Localizer.Instance.Get(
                    Loc.FetchAllVariationHashs.Progress,
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
                    Eta = Localizer.Instance[Loc.FetchAllVariationHashs.EtaUnknown];
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
            Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Cancelled];
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance.Get(Loc.Warning.FetchAllItemThumbnailsCancelled, [successCount.ToString(), failureCount.ToString(), allItems.Length.ToString()]),
                NotificationType.Warning
            );
            return;
        }

        Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Completed];

        if (failureCount == 0)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance.Get(Loc.Success.FetchAllItemThumbnails, successCount.ToString()),
                NotificationType.Success
            );
        }
        else
        {
            MainWindowViewModel.ShowNotification(
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
        Status = Localizer.Instance[Loc.FetchAllVariationHashs.Status.Cancelling];
    }
}
