using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

/// <summary>
/// KonoAsset の衣装・装飾品（着用可能アイテム）を表します。
/// </summary>
public class KonoAssetWearableItem : AbstractKonoAssetItem
{
    /// <summary>
    /// KonoAsset 側のカテゴリ名。空の場合は既定の "Wearables (KonoAsset)" として扱われます。
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 対応アバターの識別子一覧。
    /// </summary>
    [JsonPropertyName("supportedAvatars")]
    public List<string> SupportedAvatars { get; set; } = [];

    /// <summary>
    /// この KonoAsset 衣装アイテムを AvatarExplorer の Item に変換します。対応アバターも引き継がれます。
    /// </summary>
    /// <returns>変換された Item。</returns>
    public override Item ToItem()
    {
        var migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath(ItemUtils.RootFolderPrefix + Id);
        migratedItem.UpdateCategory(ItemCategory.Get(string.IsNullOrEmpty(Category) ? "Wearables (KonoAsset)" : $"{Category} (KonoAsset)" ));
        migratedItem.UpdateSupportedAvatars(SupportedAvatars);

        return migratedItem;
    }
}
