using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public class KonoAssetAvatarItem : AbstractKonoAssetItem
{
    public override Item ToItem()
    {
        Item migratedItem = ItemCreator.FromKonoAssetDescription(Description);
        migratedItem.UpdateItemPath($"<sys>{Id}");
        migratedItem.UpdateMetadata(migratedItem.Title, migratedItem.Author, migratedItem.AuthorId, migratedItem.BoothId, ItemType.Avatar, migratedItem.CustomCategory, migratedItem.ItemMemo);

        return migratedItem;
    }
}
