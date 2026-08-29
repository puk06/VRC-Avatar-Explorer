using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

/// <summary>
/// KonoAsset のワールドアイテムを表します。
/// </summary>
public class KonoAssetWorldItem : AbstractKonoAssetItem
{
    /// <summary>
    /// KonoAsset 側のカテゴリ名。空の場合は既定の "Worlds (KonoAsset)" として扱われます。
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// この KonoAsset ワールドアイテムを AvatarExplorer の Item に変換します。
    /// </summary>
    /// <returns>変換された Item。</returns>
    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath(ItemUtils.RootFolderPrefix + Id);
        migratedItem.UpdateCategory(new ItemCategory(string.IsNullOrEmpty(Category) ? "Worlds (KonoAsset)" : $"{Category} (KonoAsset)"));

        return migratedItem;
    }
}
