namespace AvatarExplorer.Core.Interfaces;

/// <summary>
/// 設定データの読み込み・更新・保存を行うリポジトリのインターフェース。
/// </summary>
/// <typeparam name="T">設定の型（パラメータレスコンストラクタを持つクラス）。</typeparam>
public interface ISettingsRepository<T> where T : class, new()
{
    /// <summary>
    /// 現在の設定インスタンス。
    /// </summary>
    T Settings { get; }

    /// <summary>
    /// 設定が変更されたときに発火するイベント。
    /// </summary>
    event Action<T>? OnSettingsChanged;

    /// <summary>
    /// ストレージから設定を読み込みます。
    /// </summary>
    void Load();

    /// <summary>
    /// 設定を更新し、変更イベントを発火します。
    /// </summary>
    /// <param name="settings">新しい設定インスタンス。</param>
    void Update(T settings);

    /// <summary>
    /// 現在の設定をストレージに保存します。
    /// </summary>
    void Save();
}
