using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External.KonoAsset;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using ErrorOr;

namespace AvatarExplorer.Core.Services.Items;

public sealed class ItemCreationResult
{
    public Item? Item { get; set; }
    public required ExtractResult ExtractResult { get; set; }
}

internal static class ItemCreator
{
    internal static async Task<ErrorOr<ItemCreationResult>> FromItemCreationContext(ItemCreationContext itemCreationContext, RuntimeSettings runtimeSettings)
    {
        ErrorOr<ExtractResult> extractResult = await FileSystemService.ExtractItemFolders(itemCreationContext, runtimeSettings.DataRootDirectory, runtimeSettings);
        if (extractResult.IsError) return Error.Failure(description: extractResult.Errors.ToErrorString());

        Item item = new()
        {
            Title = itemCreationContext.Title,
            Author = itemCreationContext.Author,
            AuthorId = itemCreationContext.AuthorId,
            BoothId = itemCreationContext.BoothId,
            ItemPath = extractResult.Value.ItemParentFolder,
            Type = itemCreationContext.ItemType,
            CustomCategory = itemCreationContext.CustomCategory
        };

        if (!string.IsNullOrEmpty(itemCreationContext.ThumbnailUrl))
        {
            bool thumbnailResult = await ImageDownloader.Fetch(itemCreationContext.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsPath, item.Id), true);
            if (thumbnailResult) item.ThumbnailFileName = item.Id;
        }

        item.UpdateSupportedAvatars(itemCreationContext.SupportedAvatars);

        return new ItemCreationResult()
        {
            Item = item,
            ExtractResult = extractResult.Value
        };
    }

    internal static Item FromKonoAssetDescription(KonoAssetDescription konoAssetDescription)
    {
        Item newItem = new()
        {
            Title = konoAssetDescription.Name,
            Author = konoAssetDescription.Creator,
            ThumbnailFileName = konoAssetDescription.ImageFilename ?? string.Empty,
            ItemMemo = konoAssetDescription.Memo ?? string.Empty,
            BoothId = konoAssetDescription.BoothItemId ?? -1,
            CreatedDate = konoAssetDescription.CreatedAt.ToString(),
            UpdatedDate = konoAssetDescription.CreatedAt.ToString()
        };

        newItem.UpdateTags(konoAssetDescription.Tags);

        return newItem;
    }
}
