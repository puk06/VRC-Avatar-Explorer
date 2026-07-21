using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetWorldItem : AbstractKonoAssetItem
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath($"<sys>{Id}");
        migratedItem.UpdateMetadata(migratedItem.Title, migratedItem.Author, migratedItem.AuthorId, migratedItem.BoothId, ItemType.Custom, string.IsNullOrEmpty(Category) ? "Worlds" : Category, migratedItem.ItemMemo);

        return migratedItem;
    }
}
