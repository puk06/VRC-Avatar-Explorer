using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Message.Avalonia;
using Message.Avalonia.Models;

namespace AvatarExplorer.UI.Services.System;

public interface IProgressReporter
{
    /// <summary>進捗を報告します。どのスレッドから呼び出しても安全で、UIスレッドへマーシャリングされます。</summary>
    void Report(string title, int progress);
}

public static class NotificationManager
{
    public static void Show(string title, string content, NotificationType type)
    {
        var manager = MessageManager.Default;
        var messageOptions = new MessageOptions
        {
            Title = title,
            Duration = TimeSpan.FromSeconds(3.5)
        };

        switch (type)
        {
            case NotificationType.Information:
                manager.ShowInformationMessage(content, messageOptions);
                break;
            case NotificationType.Success:
                manager.ShowSuccessMessage(content, messageOptions);
                break;
            case NotificationType.Warning:
                manager.ShowWarningMessage(content, messageOptions);
                break;
            case NotificationType.Error:
                manager.ShowErrorMessage(content, messageOptions);
                break;
        }
    }

    public static Task ShowWithProgress(string title, Func<IProgressReporter, Task> progressAction)
    {
        var manager = MessageManager.Default;
        var tcs = new TaskCompletionSource();

        manager
            .CreateProgress()
            .WithTitle(title)
            .WithProgress(async progress =>
            {
                try
                {
                    var reporter = new ProgressReporter(progress);
                    await progressAction(reporter);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            })
            .ShowInfo();

        return tcs.Task;
    }

    private sealed class ProgressReporter(MessageProgress progress) : IProgressReporter
    {
        private readonly MessageProgress _progress = progress;

        public void Report(string title, int progress)
        {
            Dispatcher.UIThread.Post(() => _progress.Report(title, progress));
        }
    }
}
