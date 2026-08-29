using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Services.System.Repositories;

/// <summary>設定データを管理するリポジトリの抽象基底クラスです。</summary>
/// <typeparam name="T">管理する設定の型。パラメータなしのコンストラクタを持つ必要があります。</typeparam>
public abstract class SettingsRepositoryBase<T> : ISettingsRepository<T> where T : class, new()
{
    protected readonly SettingsManager<T> Manager;

    /// <summary>現在の設定データを取得します。</summary>
    public T Settings => Manager.Settings;

    /// <summary>設定が変更されたときに発生するイベント。</summary>
    public event Action<T>? OnSettingsChanged;

    protected SettingsRepositoryBase(string filePath)
    {
        Manager = new(filePath);
        Manager.SettingsChanged += settings => OnSettingsChanged?.Invoke(settings);
    }

    /// <summary>設定を読み込みます。派生クラスで実装されます。</summary>
    public abstract void Load();

    /// <summary>指定した設定で現在の設定を更新し、保存します。</summary>
    /// <param name="settings">新しく適用する設定。</param>
    public void Update(T settings)
    {
        Manager.Update(settings);
        Manager.Save();
    }

    /// <summary>現在の設定をファイルへ保存します。</summary>
    public void Save() => Manager.Save();

    protected void InvokeSettingsChanged(T settings) => OnSettingsChanged?.Invoke(settings);
}
