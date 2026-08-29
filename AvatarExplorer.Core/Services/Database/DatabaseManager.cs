using System.Text.Json.Nodes;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.Database;

/// <summary>
/// 型 <typeparamref name="T"/> の要素を保持する JSON データベースの読み書きと、要素の追加・削除・置換を行うジェネリックな管理クラスです。
/// </summary>
/// <typeparam name="T">データベースが保持する要素の型（<see cref="IDatabaseItem"/> を実装する参照型）。</typeparam>
/// <param name="databaseFilePath">読み書き対象のデータベースファイルのパス。</param>
/// <summary>
/// 型 <typeparamref name="T"/> のアイテムを JSON ファイルとして読み書きするデータベース管理クラスです。
/// </summary>
/// <typeparam name="T">保存するアイテムの型。<see cref="IDatabaseItem"/> を実装している必要があります。</typeparam>
public class DatabaseManager<T>(string databaseFilePath)
    where T : class, IDatabaseItem
{
    /// <summary>このデータベースが読み書きするファイルのパスを取得します。</summary>
    /// <summary>このデータベースが読み書きする JSON ファイルのパス。</summary>
    public string DatabaseFilePath { get; } = databaseFilePath;

    private List<T> _items = [];
    /// <summary>データベースに読み込まれている要素の読み取り専用リストを取得します。</summary>
    /// <summary>データベースに読み込まれているアイテムの読み取り専用リスト。</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>現在のマイグレーションバージョン（スキーマバージョン）を取得または設定します。</summary>
    /// <summary>データベースのマイグレーションバージョン。読み込み時に設定され、保存時に書き込まれます。</summary>
    public int MigrationVersion { get; set; }

    /// <summary>
    /// データベースファイルから要素を読み込み、メモリ上のリストを構築します。ファイルが存在しない場合は空のリストになります。
    /// </summary>
    /// <summary>
    /// データベースファイルからアイテムを読み込みます。ファイルが存在しない場合は空のリストとして初期化されます。
    /// 配列形式とコンテナ形式（バージョン付き）の両方に対応しています。
    /// </summary>
    public void Load()
    {
        if (!File.Exists(DatabaseFilePath))
        {
            _items = [];
            return;
        }

        var json = File.ReadAllText(DatabaseFilePath);
        var root = JsonNode.Parse(json);

        if (root is JsonArray)
        {
            var items = JsonManager.Deserialize<IEnumerable<T>>(json);
            _items = (items ?? []).ToList();
            MigrationVersion = 0;
        }
        else
        {
            var container = JsonManager.Deserialize<DatabaseContainer<T>>(json);
            if (container != null)
            {
                _items = container.Items;
                MigrationVersion = container.Version;
            }
            else
            {
                _items = [];
                MigrationVersion = 0;
            }
        }
    }

    /// <summary>
    /// メモリ上の要素とマイグレーションバージョンを JSON コンテナにまとめ、データベースファイルへ保存します。
    /// </summary>
    /// <summary>
    /// 現在のアイテムとマイグレーションバージョンを、コンテナ形式の JSON としてデータベースファイルに保存します。
    /// </summary>
    public void Save()
    {
        var container = new DatabaseContainer<T>
        {
            Version = MigrationVersion,
            Items = _items
        };
        JsonFileManager<DatabaseContainer<T>>.Save(container, DatabaseFilePath);
    }

    /// <summary>
    /// 指定した ID を持つアイテムを取得します。
    /// </summary>
    /// <param name="id">検索するアイテムの ID。</param>
    /// <returns>一致するアイテム。見つからない、または id が null の場合は null。</returns>
    public T? GetById(string? id) => id == null ? null : _items.FirstOrDefault(i => i.Id == id);

    /// <summary>要素を1件追加します。</summary>
    /// <param name="item">追加する要素。</param>
    public void Add(T item) => _items.Add(item);

    /// <summary>複数の要素をまとめて追加します。</summary>
    /// <param name="items">追加する要素の列挙可能オブジェクト。</param>
    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);

    /// <summary>
    /// 指定した識別子を持つ要素をすべて削除します。
    /// </summary>
    /// <param name="id">削除する要素の識別子。</param>
    /// <returns>1件以上削除された場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;

    /// <summary>メモリ上の要素を、指定したコレクションの内容ですべて置き換えます。</summary>
    /// <param name="items">新しく設定する要素の列挙可能オブジェクト。</param>
    public void ReplaceAll(IEnumerable<T> items) => _items = items.ToList();

    /// <summary>メモリ上のすべての要素を削除します。</summary>
    public void Clear() => _items.Clear();
}
