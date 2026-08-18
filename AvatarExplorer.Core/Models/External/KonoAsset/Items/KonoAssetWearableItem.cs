using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetWearableItem : AbstractKonoAssetItem
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("supportedAvatars")]
    public List<string> SupportedAvatars { get; set; } = new List<string>();

    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath(ItemUtils.RootFolderPrefix + Id);
        migratedItem.UpdateCategory(new ItemCategory(string.IsNullOrEmpty(Category) ? "Wearables (KonoAsset)" : $"{Category} (KonoAsset)" ));
        migratedItem.UpdateSupportedAvatars(SupportedAvatars);

        return migratedItem;
    }
}
