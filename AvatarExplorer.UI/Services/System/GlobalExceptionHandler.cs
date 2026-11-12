using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.Services.System;

/// <summary>
/// アプリケーション全体で処理されなかった例外を捕捉し、<see cref="ErrorManager"/> へ内部エラーとして報告するハンドラー。
/// 報告されたエラーはログファイルへ書き出され、メインウィンドウにユーザー向けの通知が表示されます。
/// </summary>
public static class GlobalExceptionHandler
{
    private const string Tag = "Unhandled Exception";

    /// <summary>
    /// AppDomain および TaskScheduler の未処理例外ハンドラーを登録します。アプリケーション起動時に一度だけ呼び出してください。
    /// </summary>
    public static void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// UI スレッドの未処理例外ハンドラーを登録します。Avalonia の初期化後に一度だけ呼び出してください。
    /// 例外を処理済みとしてアプリケーションの動作を継続させます。
    /// </summary>
    public static void RegisterDispatcherHandler()
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            Report("An unhandled exception occurred.", null);
            return;
        }

        Report(
            e.IsTerminating
                ? "An unhandled exception occurred and the application is terminating."
                : "An unhandled exception occurred.",
            exception
        );
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // 例外を観測済みにして、GC 時にプロセスが終了しないようにする
        e.SetObserved();
        Report("An unobserved task exception occurred.", e.Exception);
    }

    private static void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // アプリケーションを終了させず、ログとエラーログ画面で確認できるようにする
        e.Handled = true;
        Report("An unhandled exception occurred on the UI thread.", e.Exception);
    }

    private static void Report(string message, Exception? exception)
    {
        try
        {
            ErrorManager.Instance.PostInternalError(message, exception, Tag);
        }
        catch (Exception ex)
        {
            // エラー報告中の例外でハンドラー自体が失敗しないようにする
            Console.WriteLine(ex);
        }

        ShowNotification();
    }

    private static void ShowNotification()
    {
        try
        {
            // どのスレッドから呼ばれてもメインウィンドウの通知として表示する
            Dispatcher.UIThread.Post(() =>
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.UnhandledException],
                    NotificationType.Error
                )
            );
        }
        catch (Exception ex)
        {
            // 通知の表示に失敗しても例外処理を妨げないようにする
            Console.WriteLine(ex);
        }
    }
}
