using System.Text.Json.Nodes;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.Database;

public class DatabaseManager<T>(string databaseFilePath)
    where T : class, IDatabaseItem
{
    public string DatabaseFilePath { get; } = databaseFilePath;

    private List<T> _items = [];
    public IReadOnlyList<T> Items => _items;

    public int MigrationVersion { get; set; }

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

    public void Save()
    {
        var container = new DatabaseContainer<T>
        {
            Version = MigrationVersion,
            Items = _items
        };
        JsonFileManager<DatabaseContainer<T>>.Save(container, DatabaseFilePath);
    }

    public T? GetById(string? id) => id == null ? null : _items.FirstOrDefault(i => i.Id == id);

    public void Add(T item) => _items.Add(item);

    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);

    public bool Remove(string id) => _items.RemoveAll(i => i.Id == id) > 0;

    public void ReplaceAll(IEnumerable<T> items) => _items = items.ToList();

    public void Clear() => _items.Clear();
}
