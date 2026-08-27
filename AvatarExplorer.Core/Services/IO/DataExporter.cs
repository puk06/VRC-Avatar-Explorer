using System.Text;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Data.Paths.External.KonoAsset;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.KonoAsset;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

internal static class DataExporter
{
    internal static async Task<ErrorOr<Success>> Export(ExportContext exportContext, ExportRequest exportRequest)
    {
        return exportRequest.ExportType switch
        {
            DataExportType.Csv => await ToCsv(exportContext, exportRequest),
            DataExportType.KonoAsset => await ToKonoAsset(exportContext, exportRequest),
            _ => Error.Unexpected(description: $"Unexpected export type: {exportRequest.ExportType}")
        };
    }

    private static async Task<ErrorOr<Success>> ToCsv(ExportContext exportContext, ExportRequest exportRequest)
    {
        try
        {
            var avatarTitleMaps = ItemUtils.GetItemTitleMaps(exportContext.Items.Where(i => i.Category.Type == ItemType.Avatar), exportContext.TempAvatars);

            var filePath = Path.Combine(exportRequest.FolderPath, $"AvatarExplorer_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            FileSystemService.PrepareFileDirectory(filePath);

            await using StreamWriter sw = new(filePath, false, Encoding.UTF8);
            await sw.WriteLineAsync("Id,Title,AuthorName,ImagePath,Category,Memo,SupportedAvatars,ImplementedAvatars,BoothId,ItemPath,Tags");

            foreach (var item in exportContext.Items)
            {
                var supportedAvatarNames = new List<string>();
                foreach (var supportedAvatarId in AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatars, exportContext.CommonAvatars, exportRequest.IncludeCommonToSupported))
                {
                    var avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, supportedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    supportedAvatarNames.Add(avatarTitle);
                }

                var implementedAvatarNames = new List<string>();
                foreach (var implementedAvatarId in item.ImplementedAvatars.Distinct())
                {
                    var avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, implementedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    implementedAvatarNames.Add(avatarTitle);
                }

                var itemId = CsvUtils.EscapeCsv(item.Id);
                var itemTitle = CsvUtils.EscapeCsv(item.Title);
                var authorName = CsvUtils.EscapeCsv(item.Author);
                var imagePath = CsvUtils.EscapeCsv(item.ThumbnailFileName);

                string categoryName;
                if (item.Category.Type == ItemType.Custom) categoryName = item.Category.CustomCategory;
                else if (exportRequest.ItemTypeLocalizer is { } localizer)
                    categoryName = await localizer(item.Category.Type) ?? item.Category.Type.ToString();
                else categoryName = item.Category.Type.ToString();

                var category = CsvUtils.EscapeCsv(categoryName);
                var memo = CsvUtils.EscapeCsv(item.ItemMemo);
                var supportedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, supportedAvatarNames));
                var implementedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, implementedAvatarNames));
                var boothId = CsvUtils.EscapeCsv(item.BoothId.ToString());
                var itemPath = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.GetFolderPaths().NaturalSort(i => Path.GetFileName(i))));
                var tags = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.Tags));

                await sw.WriteLineAsync($"{itemId},{itemTitle},{authorName},{imagePath},{category},{memo},{supportedAvatarsList},{implementedAvatarsList},{boothId},{itemPath},{tags}");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to export to csv.", ex);
            return Error.Failure(description: "Failed to export to csv.");
        }
    }

    private static async Task<ErrorOr<Success>> ToKonoAsset(ExportContext exportContext, ExportRequest exportRequest)
    {
        try
        {
            var reportProgress = exportRequest.ReportProgress;
            var dataFolderPath = exportRequest.FolderPath;
            var maxDegreeOfParallelism = exportContext.RuntimeSettings.MaxDegreeOfParallelism;
            var metadataPath = KonoAssetPath.MetadataPath(dataFolderPath);
            var dataPath = KonoAssetPath.DataPath(dataFolderPath);
            var imagesPath = KonoAssetPath.ThumbnailsPath(dataFolderPath);
            var avatarTitleMaps = ItemUtils.GetItemTitleMaps(exportContext.Items.Where(i => i.Category.Type == ItemType.Avatar), exportContext.TempAvatars);

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Export.Copying, 0));

            Directory.CreateDirectory(metadataPath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(imagesPath);

            var avatarItems = new List<KonoAssetAvatarItem>();
            var wearableItems = new List<KonoAssetWearableItem>();
            var worldItems = new List<KonoAssetWorldItem>();
            var otherItems = new List<KonoAssetOtherItem>();

            var items = exportContext.Items.ToList();
            var lastPercent = -1;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var konoAssetItem = await CreateKonoAssetItem(item, avatarTitleMaps, exportRequest.IncludeCommonToSupported, exportContext.CommonAvatars, exportRequest.ItemTypeLocalizer);

                switch (konoAssetItem)
                {
                    case KonoAssetAvatarItem avatarItem:
                        avatarItems.Add(avatarItem);
                        break;
                    case KonoAssetWearableItem wearableItem:
                        wearableItems.Add(wearableItem);
                        break;
                    case KonoAssetWorldItem worldItem:
                        worldItems.Add(worldItem);
                        break;
                    case KonoAssetOtherItem otherItem:
                        otherItems.Add(otherItem);
                        break;
                }

                await CopyItemDataAsync(item, dataPath, konoAssetItem.Id, maxDegreeOfParallelism);
                await CopyItemThumbnailAsync(item, imagesPath);

                int percent = (int)(100.0 * (i + 1) / items.Count);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Export.Copying, percent));
                }
            }

            FileSystemService.SerializeClass(new KonoAssetDatabase<KonoAssetAvatarItem> { Data = avatarItems }, KonoAssetPath.AvatarsDatabasePath(dataFolderPath));
            FileSystemService.SerializeClass(new KonoAssetDatabase<KonoAssetWearableItem> { Data = wearableItems }, KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath));
            FileSystemService.SerializeClass(new KonoAssetDatabase<KonoAssetWorldItem> { Data = worldItems }, KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath));
            FileSystemService.SerializeClass(new KonoAssetDatabase<KonoAssetOtherItem> { Data = otherItems }, KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath));

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to export to KonoAsset.", ex);
            return Error.Failure(description: "Failed to export to KonoAsset.");
        }
    }

    private static async Task<AbstractKonoAssetItem> CreateKonoAssetItem(Item item, Dictionary<string, string> avatarTitleMaps, bool includeCommonToSupported, IEnumerable<CommonAvatar> commonAvatars, Func<ItemType, ValueTask<string?>>? itemTypeLocalizer = null)
    {
        var description = CreateKonoAssetDescription(item);

        if (item.Category.Type == ItemType.Avatar)
        {
            return new KonoAssetAvatarItem
            {
                Id = item.Id,
                Description = description
            };
        }

        var supportedAvatarNames = new List<string>();
        var allSupportedAvatarIds = AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatars, commonAvatars, includeCommonToSupported);
        foreach (var avatarId in allSupportedAvatarIds)
        {
            var avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, avatarId);
            if (string.IsNullOrEmpty(avatarTitle)) continue;

            supportedAvatarNames.Add(avatarTitle);
        }

        string localizedCategory;
        if (item.Category.Type == ItemType.Custom) localizedCategory = item.Category.CustomCategory;
        else if (itemTypeLocalizer is { } localizer)
            localizedCategory = await localizer(item.Category.Type) ?? item.Category.Type.ToString();
        else localizedCategory = item.Category.Type.ToString();

        if (item.Category.Type != ItemType.Custom)
        {
            return new KonoAssetWearableItem
            {
                Id = item.Id,
                Description = description,
                Category = localizedCategory,
                SupportedAvatars = supportedAvatarNames
            };
        }

        if (item.Category.Type == ItemType.Custom && localizedCategory.Equals("World", StringComparison.OrdinalIgnoreCase))
        {
            return new KonoAssetWorldItem
            {
                Id = item.Id,
                Description = description,
                Category = localizedCategory
            };
        }

        return new KonoAssetOtherItem
        {
            Id = item.Id,
            Description = description,
            Category = localizedCategory
        };
    }

    private static KonoAssetDescription CreateKonoAssetDescription(Item item)
    {
        return new KonoAssetDescription
        {
            Name = item.Title,
            Creator = item.Author,
            ImageFilename = string.IsNullOrEmpty(item.ThumbnailFileName) ? null : item.ThumbnailFileName,
            Tags = item.Tags.ToList(),
            Memo = string.IsNullOrEmpty(item.ItemMemo) ? null : item.ItemMemo,
            BoothItemId = item.BoothId == -1 ? null : item.BoothId,
            CreatedAt = ValueParser.Long(item.CreatedDate, 0),
            PublishedAt = null
        };
    }

    private static async Task CopyItemDataAsync(Item item, string dataPath, string itemId, int maxDegreeOfParallelism = 4)
    {
        var folderPaths = item.GetFolderPaths();
        var destItemPath = Path.Combine(dataPath, itemId);

        foreach (var folderPath in folderPaths)
        {
            if (!Directory.Exists(folderPath)) continue;

            var destPath = Path.Combine(destItemPath, Path.GetFileName(folderPath));
            await FileSystemService.CopyDirectoryAsync(folderPath, destPath, maxDegreeOfParallelism);
        }
    }

    private static async Task CopyItemThumbnailAsync(Item item, string imagesPath)
    {
        if (string.IsNullOrEmpty(item.ThumbnailFileName)) return;

        var sourceThumbnailPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.ThumbnailFileName);
        if (!File.Exists(sourceThumbnailPath)) return;

        var destThumbnailPath = Path.Combine(imagesPath, item.ThumbnailFileName);
        await FileSystemService.CopyFileAsync(sourceThumbnailPath, destThumbnailPath);
    }
}
