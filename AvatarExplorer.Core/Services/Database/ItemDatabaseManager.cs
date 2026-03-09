using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class ItemDatabaseManager : IDatabaseManager<Item>
{
    private List<Item> _items { get; set; } = new();
    public IReadOnlyList<Item> Items => _items;

    public string DatabaseFilePath { get; } = SystemPath.ItemDatabasePath;
    public void Add(Item item) => _items.Add(item);
    public void AddRange(IEnumerable<Item> items) => _items.AddRange(items.ToList());
    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;
    public void Update(IEnumerable<Item> items) => _items = items.ToList();
    public void Clear() => _items.Clear();
}
