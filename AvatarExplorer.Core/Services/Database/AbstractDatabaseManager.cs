using AvatarExplorer.Core.Interfaces.Database;

namespace AvatarExplorer.Core.Services.Database;

internal abstract class AbstractDatabaseManager<T> : IDatabaseManager<T>
    where T : IDatabaseItem
{
    private List<T> _items { get; set; } = new();
    public IReadOnlyList<T> Items => _items;

    public abstract string DatabaseFilePath { get; }
    public void Add(T item) => _items.Add(item);
    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);
    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;
    public void Update(IEnumerable<T> items) => _items = items.ToList();
    public void Clear() => _items.Clear();
}
