using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class ItemRepository
{
    private readonly DatabaseManager<Item> _db = new(SystemPath.ItemDatabasePath);

    public void Load(string? path = null) => _db.Load(path);
    public IReadOnlyList<Item> GetAll() => _db.Items;
    public Item? GetById(string id) => _db.Items.FirstOrDefault(i => i.Identifier == id);
    public void Add(Item item) => _db.Add(item);
    public void Save() => _db.Save();
    public void Remove(string id) => _db.Remove(id);

    public Dictionary<string, List<string>> CategorizeItems(IEnumerable<Item> items)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var item in items)
        {
            var key = item.Type == ItemType.Custom && !string.IsNullOrWhiteSpace(item.CustomCategory)
                ? $"custom:{item.CustomCategory}"
                : $"type:{item.Type.GetLocalizationKey()}";

            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(item.Id);
        }

        return result;
    }

    
    public List<ItemFile> EnumerateItemFiles(string id)
    {
        // Root
        var item = GetById(id);
        if (item == null) return [];

        var files = new List<ItemFile>();

        // Root
        var root = item.ItemPath;
        foreach (var rootFile in FileSystemService.EnumerateFiles(root, isRecursive: false))
            files.Add(new(root, rootFile));

        foreach (var rootFolder in Directory.GetDirectories(root))
            foreach (var rootFolderFile in FileSystemService.EnumerateFiles(root, isRecursive: true))
                files.Add(new(rootFolder, rootFolderFile));

        // Other Folders
        foreach (var otherFolder in item.ItemPaths)
            foreach (var otherFile in FileSystemService.EnumerateFiles(otherFolder, isRecursive: true))
                files.Add(new(otherFolder, otherFile));

        return files;
    }
}
