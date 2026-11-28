using Avalonia;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Services.System;
using ReactiveUI.Avalonia;
using SkiaSharp;

namespace AvatarExplorer.UI;

static class Program
{
    private const string MutexName = "AvatarExplorerV2.SingleInstance";

    // 依存パッケージ更新時に managed SkiaSharp と native libSkiaSharp のバージョン不一致を
    // 起動時に検出するためのスモークテスト用環境変数 (CI から使用)。
    private const string SkiaSmokeTestEnvironmentVariable = "AVATAREXPLORER_SKIA_SMOKE_TEST";

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
        // ビルドだけでは検出できない native libSkiaSharp のバージョン不一致を早期に検出する。
        // Avalonia は初期化しないため、ディスプレイのない CI 環境でも実行できる。
        if (Environment.GetEnvironmentVariable(SkiaSmokeTestEnvironmentVariable) == "1")
        {
            RunSkiaSmokeTest();
            return;
        }

        // 処理されなかった例外をログと通知で確認できるようにする
        GlobalExceptionHandler.Register();

        // シングルインスタンスはWindowsとLinuxのみ対応
        if (ProcessUtils.IsWindows() || ProcessUtils.IsLinux())
        {
            // Single Instance Check
            if (!IsSingleInstance())
            {
                SingleInstanceService.SendToServer(args);
                _singleInstanceMutex?.Dispose();
                return;
            }

            // DB読み込みなどの起動処理を待たずにパイプサーバーを立てる。
            // 起動中に別インスタンスから転送された引数は保留され、購読時に通知される。
            SingleInstanceService.StartServer();
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// native libSkiaSharp を実際に読み込んでバージョン整合性を確認します。
    /// </summary>
    /// <remarks>
    /// SKImageInfo の静的初期化時に SkiaSharp が native ライブラリのバージョンを検証するため、
    /// 不一致の場合は TypeInitializationException が発生します。
    /// </remarks>
    private static void RunSkiaSmokeTest()
    {
        using var bitmap = new SKBitmap(1, 1);
        bitmap.Erase(SKColors.Black);

        Console.WriteLine($"SkiaSharp native: {SkiaSharpVersion.Native} (minimum required: {SkiaSharpVersion.NativeMinimum})");
    }

    /// <summary>
    /// シングルインスタンス用の Mutex を取得します。
    /// 既に他のインスタンスが所有している場合は false を返します。
    /// </summary>
    private static bool IsSingleInstance()
    {
        // Linux では Windows とは違い、"Global\" とつけないとグローバルに Mutex が出来ない。
        var mutexName = ProcessUtils.IsLinux() ? "Global\\" + MutexName : MutexName;

        // 非所有で生成したうえで待機することで、Mutex の参照を確実に保持したまま所有判定を行う。
        _singleInstanceMutex = new(false, mutexName);

        try
        {
            return _singleInstanceMutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // 前回のインスタンスが Mutex を解放せずに終了した場合も、ここで所有権を引き継いでいるため
            // 新しいインスタンスとして起動を続行する。
            return true;
        }
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
