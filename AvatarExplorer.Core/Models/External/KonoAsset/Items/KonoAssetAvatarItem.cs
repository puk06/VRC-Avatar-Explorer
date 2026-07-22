using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetAvatarItem : AbstractKonoAssetItem
{
    public override Item ToItem()
    {
        Item migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath($"<sys>{Id}");
        migratedItem.UpdateCategory(new ItemCategory(ItemType.Avatar));

        return migratedItem;
    }
}
