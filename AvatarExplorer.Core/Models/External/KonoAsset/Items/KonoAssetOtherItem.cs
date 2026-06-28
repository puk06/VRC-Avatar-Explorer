using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetOtherItem : AbstractKonoAssetItem
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.ItemPath = $"<sys>{Id}";
        migratedItem.Type = ItemType.Custom;
        migratedItem.CustomCategory = string.IsNullOrEmpty(Category) ? "Others" : Category;

        return migratedItem;
    }
}
