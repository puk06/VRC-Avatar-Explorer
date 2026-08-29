using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

/// <summary>
/// KonoAsset のアバターアイテムを表します。
/// </summary>
public class KonoAssetAvatarItem : AbstractKonoAssetItem
{
    /// <summary>
    /// この KonoAsset アバターを AvatarExplorer の Item（カテゴリ：アバター）に変換します。
    /// </summary>
    /// <returns>変換された Item。</returns>
    public override Item ToItem()
    {
        Item migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath($"<sys>{Id}");
        migratedItem.UpdateCategory(new ItemCategory(ItemType.Avatar));

        return migratedItem;
    }
}
