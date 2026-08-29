namespace AvatarExplorer.Core.Interfaces;

/// <summary>
/// 識別子を持つデータの CRUD 操作や永続化を行うリポジトリの基本インターフェース。
/// </summary>
/// <typeparam name="T">管理対象の要素型（IIdentifiable を実装）。</typeparam>
public interface IRepository<out T> where T : class, IIdentifiable
{
    /// <summary>
    /// データが更新されたときに発火するイベント。
    /// </summary>
    event Action? OnUpdated;

    /// <summary>
    /// 全要素を取得します。
    /// </summary>
    /// <returns>要素の読み取り専用一覧。</returns>
    IReadOnlyList<T> GetAll();

    /// <summary>
    /// 識別子から要素を取得します。
    /// </summary>
    /// <param name="identifier">取得対象の識別子。</param>
    /// <returns>見つかった要素。存在しない場合は null。</returns>
    T? Get(string identifier);

    /// <summary>
    /// ストレージからデータを読み込みます。
    /// </summary>
    void Load();

    /// <summary>
    /// 指定した識別子の要素を削除します。
    /// </summary>
    /// <param name="identifier">削除対象の識別子。</param>
    void Remove(string identifier);

    /// <summary>
    /// 現在のデータをストレージに保存します。
    /// </summary>
    void Save();

    /// <summary>
    /// 全データをクリアします。
    /// </summary>
    void Clear();

    /// <summary>
    /// データが変更されたことをマークし、必要に応じて通知を行います。
    /// </summary>
    void MarkAsChanged();
}
