using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class BulkImportPresetRepository
{
    private readonly DatabaseManager<BulkImportPreset> _db = new(SystemPath.BulkImportPresetDatabasePath);

    /// <summary>
    /// アイテムが追加・更新・削除された際に発火します。
    /// </summary>
    public event Action? OnUpdated;

    public void Load()
    {
        DatabaseMigrationService.Migrate(
            _db.DatabaseFilePath,
            DatabaseMigrations.BulkImportPresetVersion,
            DatabaseMigrations.ApplyBulkImportPresetMigration);

        _db.Load();
        OnUpdated?.Invoke();
    }

    public IReadOnlyList<BulkImportPreset> GetAll() => _db.Items;
    public BulkImportPreset? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        _db.Remove(item.Id);
        Save();

        OnUpdated?.Invoke();
    }

    public void Create(string presetName, BulkImportItem[] items)
    {
        var group = new BulkImportPreset(presetName);
        group.UpdateItems(items);

        _db.Add(group);
        Save();

        OnUpdated?.Invoke();
    }

    public void Save() => _db.Save();

    public void MarkAsChanged() => OnUpdated?.Invoke();
}
