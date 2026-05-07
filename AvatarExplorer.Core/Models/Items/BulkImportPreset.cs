using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class BulkImportPreset : AbstractDatabaseItem, ISelectableItem
{
    public string PresetName { get; set; } = string.Empty;
    [JsonInclude] private List<BulkImportItem> Items { get; set; } = new List<BulkImportItem>();
    [JsonIgnore] public ImmutableArray<BulkImportItem> ItemsView => Items.ToImmutableArray();

    public void UpdateItems(IEnumerable<BulkImportItem> items) => Items = items.ToList();
}
