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
        Item item = new()
        {
            Title = itemCreationContext.Title,
            Author = itemCreationContext.Author,
            AuthorId = itemCreationContext.AuthorId,
            BoothId = itemCreationContext.BoothId,
            Type = itemCreationContext.ItemType,
            CustomCategory = itemCreationContext.CustomCategory,
            ItemMemo = itemCreationContext.ItemMemo
        };

        ErrorOr<ExtractResult> extractResult = await FileSystemService.ExtractItemFolders(itemCreationContext, runtimeSettings.DataRootDirectory, runtimeSettings, item.Id);
        if (extractResult.IsError) return Error.Failure(description: extractResult.Errors.ToErrorString());

        if (!string.IsNullOrEmpty(itemCreationContext.ThumbnailUrl))
        {
            bool thumbnailResult = await ImageDownloader.Fetch(itemCreationContext.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id), true);
            if (thumbnailResult) item.ThumbnailFileName = item.Id;
        }

        item.ItemPath = extractResult.Value.ItemParentFolder;
        item.UpdateItemPaths(extractResult.Value.FolderPaths);
        item.UpdateSupportedAvatars(itemCreationContext.SupportedAvatars);
        item.UpdateTags(itemCreationContext.Tags);

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
