using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

/// <summary>識別子付きデータベースアイテムを管理するリポジトリの抽象基底クラスです。</summary>
/// <typeparam name="T">管理するアイテムの型。IIdentifiableおよびIDatabaseItemを実装している必要があります。</typeparam>
/// <param name="dbPath">データベースファイルのパス。</param>
public abstract class RepositoryBase<T>(string dbPath) : IRepository<T> where T : class, IIdentifiable, IDatabaseItem
{
    protected readonly DatabaseManager<T> Db = new(dbPath);

    /// <summary>リポジトリの内容が変更されたときに発生するイベント。</summary>
    public event Action? OnUpdated;

    /// <summary>データベース内の全アイテムを取得します。</summary>
    /// <returns>全アイテムの読み取り専用リスト。</returns>
    public IReadOnlyList<T> GetAll() => Db.Items;

    /// <summary>指定したIdentifierを持つアイテムを取得します。</summary>
    /// <param name="identifier">検索するアイテムのIdentifier。</param>
    /// <returns>見つかったアイテム。見つからない場合はnull。</returns>
    public T? Get(string identifier) => Db.Items.FirstOrDefault(i => i.Identifier == identifier);

    /// <summary>指定したIdentifierのアイテムを削除します。</summary>
    /// <param name="identifier">削除するアイテムのIdentifier。</param>
    public virtual void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        Db.Remove(item.Id);
        Db.Save();
        OnUpdated?.Invoke();
    }

    /// <summary>データベースへの変更を保存（ファイルへ書き込み）します。</summary>
    public void Save() => Db.Save();

    /// <summary>全アイテムを削除し、データベースを空の状態にして保存します。</summary>
    public void Clear()
    {
        Db.Clear();
        Db.Save();
        OnUpdated?.Invoke();
    }

    /// <summary>データが変更されたことを通知し、OnUpdatedイベントを発行します。</summary>
    public void MarkAsChanged() => OnUpdated?.Invoke();

    /// <summary>データベースからデータを読み込みます。派生クラスで実装されます。</summary>
    public abstract void Load();

    protected void InvokeUpdated() => OnUpdated?.Invoke();

    /// <summary>新しいアイテムをデータベースに追加し、保存して変更を通知します。</summary>
    /// <param name="item">追加するアイテム。</param>
    public void Add(T item)
    {
        Db.Add(item);
        Db.Save();
        OnUpdated?.Invoke();
    }
}
