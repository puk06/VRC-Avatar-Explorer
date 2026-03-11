using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class BulkImportPreset : ISelectableItem, IDatabaseItem
{
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string PresetName { get; set; } = string.Empty;
    [JsonInclude] private List<BulkImportItem> Items { get; set; } = new List<BulkImportItem>();
    public IReadOnlyList<BulkImportItem> ItemsView => Items;

    public void UpdateItems(IEnumerable<BulkImportItem> items) => Items = items.ToList();
}
