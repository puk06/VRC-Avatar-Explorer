using AvatarExplorer.Core.Models.External.KonoAsset;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemCreator
{
    internal static Item FromKonoAssetDescription(KonoAssetDescription konoAssetDescription)
    {
        var newItem = new Item();
        newItem.UpdateMetadata(
            konoAssetDescription.Name,
            konoAssetDescription.Creator,
            string.Empty,
            konoAssetDescription.BoothItemId ?? -1,
            new ItemCategory(ItemType.None),
            konoAssetDescription.Memo ?? string.Empty
        );
        newItem.UpdateThumbnailFileName(konoAssetDescription.ImageFilename ?? string.Empty);
        newItem.SetCreationDates(konoAssetDescription.CreatedAt.ToString(), konoAssetDescription.CreatedAt.ToString());
        newItem.UpdateTags(konoAssetDescription.Tags);

        return newItem;
    }
}
