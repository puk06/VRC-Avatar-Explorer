using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

/// <summary>
/// KonoAsset の「その他」アイテム（衣装やアバター以外）を表します。
/// </summary>
public class KonoAssetOtherItem : AbstractKonoAssetItem
{
    /// <summary>
    /// KonoAsset 側のカテゴリ名。空の場合は既定の "Others (KonoAsset)" として扱われます。
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// この KonoAsset アイテムを AvatarExplorer の Item に変換します。
    /// </summary>
    /// <returns>変換された Item。</returns>
    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath(ItemUtils.RootFolderPrefix + Id);
        migratedItem.UpdateCategory(ItemCategory.Get(string.IsNullOrEmpty(Category) ? "Others (KonoAsset)" : $"{Category} (KonoAsset)" ));

        return migratedItem;
    }
}
