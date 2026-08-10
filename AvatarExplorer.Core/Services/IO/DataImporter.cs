using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Data.Paths.External.KonoAsset;
using AvatarExplorer.Core.Data.Paths.External.V1;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.KonoAsset;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;
using AvatarExplorer.Core.Models.External.V1;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

public class DataImporter(ItemRepository items, CommonAvatarRepository commonAvatars, TempAvatarRepository tempAvatars)
{
    private const string V1DatasFolderName = "Datas";
    private static readonly string V1ItemsFolderPrefix = $"{V1DatasFolderName}\\Items\\";
    private static readonly string V1ThumbnailFolderPrefix = $"{V1DatasFolderName}\\Thumbnail\\";

    private readonly ItemRepository _items = items;
    private readonly CommonAvatarRepository _commonAvatars = commonAvatars;
    private readonly TempAvatarRepository _tempAvatars = tempAvatars;

    public async Task<ErrorOr<Success>> Import(ImportRequest importRequest)
    {
        var type = importRequest.ImportType;
        var source = type & DataImportType.SourceMask;

        if (source == DataImportType.None)
            return Error.Unexpected(description: "No import source type specified.");

        if (type.HasFlag(DataImportType.Items))
        {
            var result = source switch
            {
                DataImportType.V1 => await FromV1(importRequest),
                DataImportType.KonoAsset => await FromKonoAsset(importRequest),
                _ => Error.Unexpected(description: $"Unexpected import source: {source}")
            };
            if (result.IsError) return result;
        }

        if (type.HasFlag(DataImportType.Thumbnails))
        {
            var result = source switch
            {
                DataImportType.V1 => await FromV1Thumbnail(_items.GetAll(), importRequest.DataFolderPath, importRequest.ReportProgress),
                DataImportType.KonoAsset => await FromKonoAssetThumbnail(_items.GetAll(), importRequest.DataFolderPath, importRequest.ReportProgress),
                _ => Error.Unexpected(description: $"Unexpected import source: {source}")
            };
            if (result.IsError) return result;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> FromV1(ImportRequest importRequest)
    {
        try
        {
            var reportProgress = importRequest.ReportProgress;
            var shouldCopyAsset = importRequest.CopyAssetData;
            var dataFolderPath = importRequest.DataFolderPath;

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 0));

            if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName)))
                dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

            var v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
            var v1CommonAvatars = FileSystemService.DeserializeClass<List<CommonAvatarV1>>(SystemPathV1.CommonAvatarDatabasePath(dataFolderPath)).Value ?? [];

            var pathMapping = new Dictionary<string, string>();
            var lastPercent = -1;

            for (int i = 0; i < v1Items.Count; i++)
            {
                var v1Item = v1Items[i];
                var previousItemPath = v1Item.ItemPath;

                var item = CreateItemFromItemV1(v1Item);
                _items.Add(item);

                var sourcePaths = new List<string>
                {
                    GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateV1Path(v1Item.ItemPath))
                };

                if (!string.IsNullOrEmpty(v1Item.MaterialPath))
                    sourcePaths.Add(GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateV1Path(v1Item.MaterialPath)));

                await _items.AddPaths(item.Identifier, sourcePaths.Select(p => new ItemPathEntry { FileName = Path.GetFileName(p), Path = p }), !shouldCopyAsset, false);

                var sourceThumbnailPath = GetItemPath(SystemPathV1.ItemThumbnailsPath(dataFolderPath), MigrateV1Path(v1Item.ImagePath));
                var destThumbnailPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
                var thumbnailResult = await FileSystemService.CopyFileAsync(sourceThumbnailPath, destThumbnailPath);
                item.UpdateThumbnailFileName(thumbnailResult.IsError ? string.Empty : item.Id);

                pathMapping[previousItemPath] = item.Identifier;

                int percent = (int)(100.0 * i / v1Items.Count);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, percent));
                }
            }

            foreach (var newItemId in pathMapping.Values)
            {
                var item = _items.Get(newItemId);
                if (item == null) continue;

                var supportedAvatars = item.SupportedAvatars.Select(a => pathMapping.TryGetValue(a, out var mapped) ? mapped : a);
                item.UpdateSupportedAvatars(supportedAvatars);

                var implementedAvatars = item.ImplementedAvatars.Select(a => pathMapping.TryGetValue(a, out var mapped) ? mapped : a);
                item.UpdateImplementedAvatars(implementedAvatars);
            }

            foreach (var v1CommonAvatar in v1CommonAvatars)
            {
                var commonAvatar = CreateCommonAvatarFromV1(v1CommonAvatar);
                var avatarPaths = commonAvatar.Avatars.Select(a => pathMapping.TryGetValue(a, out var mapped) ? mapped : a);
                commonAvatar.UpdateAvatars(avatarPaths);
                _commonAvatars.Add(commonAvatar);
            }

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 100));

            _items.Save();
            _items.MarkAsChanged();
            _commonAvatars.Save();
            _commonAvatars.MarkAsChanged();

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data from v1.", ex);
            return Error.Failure(description: "Failed to import data from v1.");
        }
    }

    private static Item CreateItemFromItemV1(ItemV1 v1Item)
    {
        var item = new Item();
        item.UpdateMetadata(
            v1Item.Title,
            v1Item.AuthorName,
            v1Item.AuthorId,
            v1Item.BoothId,
            new ItemCategory((ItemType)(v1Item.Type + 1), v1Item.CustomCategory),
            v1Item.ItemMemo
        );
        item.SetCreationDates(v1Item.CreatedDate, v1Item.UpdatedDate);
        item.UpdateSupportedAvatars(v1Item.SupportedAvatar);
        item.UpdateImplementedAvatars(v1Item.ImplementedAvatars);
        item.UpdateTags(v1Item.Tags);
        return item;
    }

    private static CommonAvatar CreateCommonAvatarFromV1(CommonAvatarV1 v1CommonAvatar)
    {
        var commonAvatar = new CommonAvatar(v1CommonAvatar.Name);
        commonAvatar.UpdateAvatars(v1CommonAvatar.Avatars);
        return commonAvatar;
    }

    private static string MigrateV1Path(string path)
    {
        if (path.StartsWith("./")) path = path[2..];

        if (path.StartsWith(V1ItemsFolderPrefix, StringComparison.Ordinal))
            return path.Replace(V1ItemsFolderPrefix, "<sys>");
        if (path.StartsWith(V1ThumbnailFolderPrefix, StringComparison.Ordinal))
            return path.Replace(V1ThumbnailFolderPrefix, "<sys>");

        return path;
    }

    private async Task<ErrorOr<Success>> FromKonoAsset(ImportRequest importRequest)
    {
        try
        {
            var reportProgress = importRequest.ReportProgress;
            var shouldCopyAsset = importRequest.CopyAssetData;
            var dataFolderPath = importRequest.DataFolderPath;

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 0));

            List<AbstractKonoAssetItem> konoAssetItems =
            [
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetAvatarItem>>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetWearableItem>>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetWorldItem>>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetOtherItem>>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
            ];

            var avatarNameMap = new Dictionary<string, string>();
            foreach (var avatarName in konoAssetItems.OfType<KonoAssetWearableItem>().SelectMany(i => i.SupportedAvatars).Distinct())
            {
                if (avatarNameMap.ContainsKey(avatarName)) continue;

                var tempAvatar = new TempAvatar(avatarName);
                avatarNameMap.Add(avatarName, tempAvatar.Identifier);
                _tempAvatars.Add(tempAvatar);
            }

            var lastPercent = -1;
            for (int i = 0; i < konoAssetItems.Count; i++)
            {
                var konoAssetItem = konoAssetItems[i];
                var item = konoAssetItem.ToItem();
                item.UpdateItemPath(string.Empty);
                _items.Add(item);

                var sourcePath = Path.Combine(KonoAssetPath.DataPath(dataFolderPath), konoAssetItem.Id);

                if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
                {
                    ErrorManager.Instance.PostInternalError($"Source path not found: {sourcePath}");
                    continue;
                }

                var targetPaths = Directory.GetDirectories(sourcePath).Concat(Directory.GetFiles(sourcePath))
                    .Select(p => new ItemPathEntry { FileName = Path.GetFileName(p), Path = p })
                    .ToList();

                // 移行時は絶対にOriginalを消さないようにする
                await _items.AddPaths(item.Identifier, targetPaths, !shouldCopyAsset, false);

                if (!string.IsNullOrEmpty(konoAssetItem.Description.ImageFilename))
                {
                    var sourceThumbnailPath = Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), konoAssetItem.Description.ImageFilename);
                    var destThumbnailPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
                    var thumbnailResult = await FileSystemService.CopyFileAsync(sourceThumbnailPath, destThumbnailPath);
                    item.UpdateThumbnailFileName(thumbnailResult.IsError ? string.Empty : item.Id);
                }

                item.UpdateSupportedAvatars(item.SupportedAvatars.Select(a => avatarNameMap[a]));

                int percent = (int)(100.0 * i / konoAssetItems.Count);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, percent));
                }
            }

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 100));

            _items.Save();
            _items.MarkAsChanged();

            _tempAvatars.Save();
            _tempAvatars.MarkAsChanged();

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data from KonoAsset.", ex);
            return Error.Failure(description: "Failed to import data from KonoAsset.");
        }
    }

    private async Task<ErrorOr<Success>> FromV1Thumbnail(
        IEnumerable<Item> currentItems,
        string dataFolderPath,
        Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 0));

            if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName)))
                dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

            var v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
            var sourceThumbnailMap = new Dictionary<int, string>();

            foreach (var sourceItem in v1Items)
            {
                if (sourceItem.BoothId == -1 || string.IsNullOrWhiteSpace(sourceItem.ImagePath)) continue;
                if (sourceThumbnailMap.ContainsKey(sourceItem.BoothId)) continue;

                var thumbnailPath = GetItemPath(
                    SystemPathV1.ItemThumbnailsPath(dataFolderPath),
                    MigrateV1Path(sourceItem.ImagePath));

                if (File.Exists(thumbnailPath))
                    sourceThumbnailMap[sourceItem.BoothId] = thumbnailPath;
            }

            await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);
            _items.Save();

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import thumbnails from v1.", ex);
            return Error.Failure(description: "Failed to import thumbnails from v1.");
        }
    }

    private async Task<ErrorOr<Success>> FromKonoAssetThumbnail(
        IEnumerable<Item> currentItems,
        string dataFolderPath,
        Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 0));

            List<AbstractKonoAssetItem> konoAssetItems =
            [
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetAvatarItem>>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetWearableItem>>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetWorldItem>>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetDatabase<KonoAssetOtherItem>>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
            ];

            var sourceThumbnailMap = new Dictionary<int, string>();
            foreach (var sourceItem in konoAssetItems)
            {
                if (string.IsNullOrWhiteSpace(sourceItem.Description.ImageFilename)) continue;

                var item = sourceItem.ToItem();
                if (item.BoothId == -1 || sourceThumbnailMap.ContainsKey(item.BoothId)) continue;

                var thumbnailPath = Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), sourceItem.Description.ImageFilename!);
                if (File.Exists(thumbnailPath))
                    sourceThumbnailMap[item.BoothId] = thumbnailPath;
            }

            await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);
            _items.Save();

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import thumbnails from KonoAsset.", ex);
            return Error.Failure(description: "Failed to import thumbnails from KonoAsset.");
        }
    }

    private static async Task ApplyThumbnailMap(
        IEnumerable<Item> currentItems,
        Dictionary<int, string> sourceThumbnailMap,
        Func<(string, int), Task>? reportProgress = null)
    {
        var targets = currentItems.Where(i => i.BoothId != -1).ToArray();
        if (targets.Length == 0)
        {
            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, 100));
            return;
        }

        var lastPercent = -1;
        for (int i = 0; i < targets.Length; i++)
        {
            var targetItem = targets[i];
            if (sourceThumbnailMap.TryGetValue(targetItem.BoothId, out var sourcePath))
            {
                var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, targetItem.Id);
                var copyResult = await FileSystemService.CopyFileAsync(sourcePath, destPath);
                if (!copyResult.IsError)
                    targetItem.UpdateThumbnailFileName(targetItem.Id);
            }

            var percent = (int)(100.0 * (i + 1) / targets.Length);
            if (percent != lastPercent)
            {
                lastPercent = percent;
                if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Import.Copying, percent));
            }
        }
    }

    private static string GetItemPath(string parentFolder, string itemPath)
    {
        // <sys>で始まっていたら相対パスと認識して親フォルダに置き換える
        // 始まっていないものはフルパスと認識してそのまま変えす
        return itemPath.StartsWith("<sys>") ? Path.Join(parentFolder, itemPath.Replace("<sys>", string.Empty)) : itemPath;
    }
}
