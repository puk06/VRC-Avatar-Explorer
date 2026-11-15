using System.Diagnostics;
using AvatarExplorer.UI.Interfaces;

namespace AvatarExplorer.UI.Services.System;

/// <summary>
/// 起動時に初期化が必要なインスタンスを優先度順にまとめて初期化するレジストリです。
/// デバッグビルドでは各初期化処理にかかった時間をデバッグ出力へ書き出します。
/// </summary>
public static class InitializableRegistry
{
    private static readonly List<(int, IInitializable)> _instances = [];
    private static readonly List<(int, IPostInitializable)> _postInstances = [];
    private static bool _completed;

    /// <summary>
    /// 初期化対象のインスタンスを優先度付きで登録します。優先度の数値が小さいものから先に初期化されます。
    /// </summary>
    /// <param name="priority">初期化の優先度。</param>
    /// <param name="instance">初期化対象のインスタンス。</param>
    public static void Register(int priority, IInitializable instance)
    {
        _instances.Add((priority, instance));
    }

    /// <summary>
    /// 初期化後に実行する対象のインスタンスを優先度付きで登録します。優先度の数値が小さいものから先に実行されます。
    /// </summary>
    /// <param name="priority">初期化後の処理の優先度。</param>
    /// <param name="instance">初期化後の処理の対象となるインスタンス。</param>
    public static void Register(int priority, IPostInitializable instance)
    {
        _postInstances.Add((priority, instance));
    }

    /// <summary>
    /// 登録されたインスタンスの初期化を優先度順に実行します。二回目以降の呼び出しは無視されます。
    /// </summary>
    public static async Task Complete()
    {
        if (_completed) return;
        _completed = true;

        long totalStartTimestamp = Stopwatch.GetTimestamp();

        foreach (var instance in _instances.OrderBy(i => i.Item1))
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            await instance.Item2.Initialize();
            LogElapsedTime(nameof(IInitializable.Initialize), instance.Item1, instance.Item2, startTimestamp);
        }

        foreach (var instance in _postInstances.OrderBy(i => i.Item1))
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            await instance.Item2.OnInitialized();
            LogElapsedTime(nameof(IPostInitializable.OnInitialized), instance.Item1, instance.Item2, startTimestamp);
        }

        LogElapsedTime(nameof(Complete), null, null, totalStartTimestamp);
    }

    /// <summary>
    /// 指定した処理にかかった時間をデバッグ出力へ書き出します。リリースビルドでは呼び出し自体が除去されます。
    /// </summary>
    /// <param name="phase">計測対象の処理名。</param>
    /// <param name="priority">対象インスタンスの優先度。全体の計測の場合は null。</param>
    /// <param name="instance">計測対象のインスタンス。全体の計測の場合は null。</param>
    /// <param name="startTimestamp">計測開始時のタイムスタンプ。</param>
    [Conditional("DEBUG")]
    private static void LogElapsedTime(string phase, int? priority, object? instance, long startTimestamp)
    {
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        string target = instance?.GetType().FullName ?? "All";
        string priorityText = priority.HasValue ? $" (Priority: {priority.Value})" : string.Empty;

        Debug.WriteLine($"[InitializableRegistry] {phase}{priorityText}: {target} - {elapsed.TotalMilliseconds:F2} ms");
    }
}
