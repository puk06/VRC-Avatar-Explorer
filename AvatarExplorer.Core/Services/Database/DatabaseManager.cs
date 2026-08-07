using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.Database;

public class DatabaseManager<T>(string databaseFilePath)
    where T : class, IDatabaseItem
{
    public string DatabaseFilePath { get; } = databaseFilePath;

    private List<T> _items = [];
    public IReadOnlyList<T> Items => _items;

    public void Load() => ReplaceAll(JsonFileManager<IEnumerable<T>>.Load(DatabaseFilePath) ?? []);

    public void Save() => JsonFileManager<IEnumerable<T>>.Save(_items, DatabaseFilePath);

    public T? GetById(string? id) => id == null ? null : _items.FirstOrDefault(i => i.Id == id);

    public void Add(T item) => _items.Add(item);

    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);

    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;

    public void ReplaceAll(IEnumerable<T> items) => _items = items.ToList();

    public void Clear() => _items.Clear();
}
