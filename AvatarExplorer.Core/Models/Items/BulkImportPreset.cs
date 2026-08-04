using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class BulkImportPreset(string presetName) : AbstractDatabaseItem, INavigationable
{
    [JsonInclude] public string PresetName { get; private set; } = presetName;
    [JsonInclude] public ImmutableArray<BulkImportItem> Items { get; private set; } = [];

    public void UpdateItems(IEnumerable<BulkImportItem> items) => Items = items.ToImmutableArray();

    public string Identifier => "bulkimportpreset:" + Id;
}
