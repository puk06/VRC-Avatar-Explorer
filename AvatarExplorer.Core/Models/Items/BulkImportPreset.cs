using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class BulkImportPreset : AbstractDatabaseItem, ISelectableItem
{
    public string PresetName { get; set; } = string.Empty;
    [JsonInclude] public ImmutableArray<BulkImportItem> Items { get; private set; } = [];

    public void UpdateItems(IEnumerable<BulkImportItem> items) => Items = items.ToImmutableArray();

    public string Identifier => "bulkimportpreset:" + Id;
}
