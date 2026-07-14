using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class BulkImportPresetRepository
{
    private readonly DatabaseManager<BulkImportPreset> _db = new(SystemPath.BulkImportPresetDatabasePath);

    public void Load(string? path = null) => _db.Load(path);
    public IReadOnlyList<BulkImportPreset> GetAll() => _db.Items;
    public BulkImportPreset? GetById(string id) => _db.GetById(id);
    public void Add(BulkImportPreset preset) => _db.Add(preset);
    public void Remove(string id) => _db.Remove(id);
}
