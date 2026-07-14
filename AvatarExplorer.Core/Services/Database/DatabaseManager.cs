using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.Database;

internal class DatabaseManager<T>(string databaseFilePath)
    where T : class, IDatabaseItem
{
    public string DatabaseFilePath { get; } = databaseFilePath;

    private List<T> _items = [];
    public IReadOnlyList<T> Items => _items;

    public void Load(string? path = null) => ReplaceAll(JsonFileManager<IEnumerable<T>>.Load(path ?? DatabaseFilePath) ?? []);

    public void Save() => JsonFileManager<IEnumerable<T>>.Save(_items, DatabaseFilePath);

    public T? GetById(string? id) => id == null ? null : _items.FirstOrDefault(i => i.Id == id);

    public void Add(T item, bool save = true)
    {
        _items.Add(item);
        if (save) Save();
    }

    public void AddRange(IEnumerable<T> items, bool save = true)
    {
        _items.AddRange(items);
        if (save) Save();
    }

    public bool Remove(string id, bool save = true)
    {
        var result = _items.RemoveAll(i => i.Id == id) > 0;
        if (save) Save();
        return result;
    }

    public void ReplaceAll(IEnumerable<T> items) => _items = items.ToList();
    public void Clear() => _items.Clear();
}
