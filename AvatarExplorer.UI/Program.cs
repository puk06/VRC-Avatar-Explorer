using Avalonia;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Services.System;
using ReactiveUI.Avalonia;

namespace AvatarExplorer.UI;

static class Program
{
    private const string MutexName = "AvatarExplorerV2.SingleInstance";

    // Mutex をローカル変数で保持すると、Release / ReadyToRun ビルドでは JIT が最後の使用以降を
    // 不要とみなして GC 対象にし、ファイナライザが OS ハンドルを閉じて名前付き Mutex が消滅する。
    // その結果、次のプロセスが新しい Mutex を作れてしまい多重起動を防げなくなるため、静的に保持する。
    private static Mutex? _singleInstanceMutex;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 処理されなかった例外をログと通知で確認できるようにする
        GlobalExceptionHandler.Register();

        // シングルインスタンスはWindowsとLinuxのみ対応
        if (ProcessUtils.IsWindows() || ProcessUtils.IsLinux())
        {
            // Linux では Windows とは違い、"Global\" とつけないとグローバルに Mutex が出来ない。
            var mutexName = ProcessUtils.IsLinux() ? "Global\\" + MutexName : MutexName;

            // Single Instance Check
            _singleInstanceMutex = new(true, mutexName, out bool isNew);

            if (!isNew)
            {
                SingleInstanceService.SendToServer(args);
                return;
            }

            // DB読み込みなどの起動処理を待たずにパイプサーバーを立てる。
            // 起動中に別インスタンスから転送された引数は保留され、購読時に通知される。
            SingleInstanceService.StartServer();
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .UseReactiveUI(_ => {})
            .RegisterReactiveUIViewsFromEntryAssembly()
            .LogToTrace();
}
