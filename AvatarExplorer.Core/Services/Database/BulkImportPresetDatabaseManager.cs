using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class BulkImportPresetDatabaseManager : IDatabaseManager<BulkImportPreset>
{
    private List<BulkImportPreset> _presets { get; set; } = new();
    public IReadOnlyList<BulkImportPreset> Items => _presets;

    public string DatabaseFilePath { get; } = SystemPath.BulkImportPresetDatabasePath;
    public void Add(BulkImportPreset preset) => _presets.Add(preset);
    public void AddRange(IEnumerable<BulkImportPreset> preset) => _presets.AddRange(preset);
    public bool Remove(string id) => _presets.RemoveAll(i => i.Id == id) > 0;
    public void Update(IEnumerable<BulkImportPreset> preset) => _presets = preset.ToList();
    public void Clear() => _presets.Clear();
}
