using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.External.KonoAsset;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetAvatarItem : IKonoAssetItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public KonoAssetDescription Description { get; set; } = new KonoAssetDescription();

    public Item ToItem()
    {
        Item migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.ItemPath = $"<sys>{Id}";
        migratedItem.Type = ItemType.Avatar;

        return migratedItem;
    }
}
