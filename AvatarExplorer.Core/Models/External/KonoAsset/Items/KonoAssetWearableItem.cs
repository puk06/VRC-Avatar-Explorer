using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.External.KonoAsset;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetWearableItem : IKonoAssetItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public KonoAssetDescription Description { get; set; } = new KonoAssetDescription();

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("supportedAvatars")]
    public List<string> SupportedAvatars { get; set; } = new List<string>();

    public Item ToItem()
    {
        Item migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.ItemPath = $"<sys>{Id}";
        migratedItem.Type = ItemType.Custom;
        migratedItem.CustomCategory = string.IsNullOrEmpty(Category) ? "Wearables" : Category;
        migratedItem.UpdateSupportedAvatars(SupportedAvatars);

        return migratedItem;
    }
}
