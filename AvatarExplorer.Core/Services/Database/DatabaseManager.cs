using System.Collections.Immutable;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.Database;

internal class DatabaseManager<T>(string databaseFilePath)
    where T : IDatabaseItem
{
    public string DatabaseFilePath { get; } = databaseFilePath;

    private List<T> _items { get; set; } = new();
    public ImmutableArray<T> Items => _items.ToImmutableArray();

    public void Load(string? path = null) => Update(JsonFileManager<IEnumerable<T>>.Load(path ?? DatabaseFilePath) ?? []);
    public void Save() => JsonFileManager<IEnumerable<T>>.Save(_items, DatabaseFilePath);
    
    public T? GetById(string id) => _items.FirstOrDefault(i => i.Id == id);

    public void Add(T item) => _items.Add(item);
    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);
    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;
    public void Update(IEnumerable<T> items) => _items = items.ToList();
    public void Clear() => _items.Clear();
}
