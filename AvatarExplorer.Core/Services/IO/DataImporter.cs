using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Data.Paths.External.KonoAsset;
using AvatarExplorer.Core.Data.Paths.External.V1;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.KonoAsset.Databases;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;
using AvatarExplorer.Core.Models.External.V1;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

internal static class DataImporter
{
    private const string V1DatasFolderName = "Datas";
    private static readonly string V1ItemsFolderPrefix = $"{V1DatasFolderName}\\Items\\";
    private static readonly string V1ThumbnailFolderPrefix = $"{V1DatasFolderName}\\Thumbnail\\";

    private static int GetImportParallelism(RuntimeSettings runtimeSettings)
    {
        int requested = runtimeSettings.MaxDegreeOfParallelism;
        int cappedByCpu = Math.Max(1, Environment.ProcessorCount - 1);
        return Math.Clamp(requested - 1, 1, cappedByCpu);
    }

    internal static async Task<ErrorOr<DataImportResult>> Import(DataImportType importType, string dataFolderPath, Dictionary<ItemType, string> localizedItemTypesMapping, bool copyAssetData, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        return importType switch
        {
            DataImportType.V1 => await FromV1(dataFolderPath, copyAssetData, runtimeSettings, reportProgress),
            DataImportType.KonoAsset => await FromKonoAsset(dataFolderPath, localizedItemTypesMapping, copyAssetData, runtimeSettings, reportProgress),
            _ => Error.Unexpected(description: $"Unexpected import type: {importType}")
        };
    }

    internal static async Task<ErrorOr<Success>> ImportThumbnail(ThumbnailImportType importType, IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    {
        return importType switch
        {
            ThumbnailImportType.V1 => await FromV1Thumbnail(currentItems, dataFolderPath, reportProgress),
            ThumbnailImportType.KonoAsset => await FromKonoAssetThumbnail(currentItems, dataFolderPath, reportProgress),
            _ => Error.Unexpected(description: $"Unexpected thumbnail import type: {importType}")
        };
    }
    
    private static async Task<ErrorOr<DataImportResult>> FromV1(string dataFolderPath, bool copyAssetData, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            DataImportResult dataImportResult = new();
            int importParallelism = GetImportParallelism(runtimeSettings);
        
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

            // AEソフト本体のフォルダが渡された時はパスを変換して上げる
            if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName))) dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

            List<ItemV1> v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
            List<CommonAvatarV1> v1CommonAvatars = FileSystemService.DeserializeClass<List<CommonAvatarV1>>(SystemPathV1.CommonAvatarDatabasePath(dataFolderPath)).Value ?? [];

            List<Item> items = new();

            Dictionary<string, string> pathMapping = new();

            // データ移行処理
            int lastPercent = -1;
            for (int i = 0; i < v1Items.Count; i++)
            {
                ItemV1 item = v1Items[i];
                string previousItemPath = item.ItemPath;
                
                Item newItem = CreateItemFromItemV1(item);

                if (copyAssetData)
                {
                    string safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
                    string newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true) ?? throw new DirectoryNotFoundException("Counldn't get unique item path");
                    
                    await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ItemPath)), newItemPath, importParallelism);
                    if (!string.IsNullOrEmpty(item.MaterialPath)) await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.MaterialPath)), newItemPath, importParallelism);
                
                    newItem.ItemPath = $"<sys>{Path.GetRelativePath(runtimeSettings.DataRootDirectory, newItemPath)}";
                }
                else
                {
                    string newItemPath = ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ItemPath));
                    if (!Directory.Exists(newItemPath)) throw new DirectoryNotFoundException($"Item path not found: {newItemPath}");

                    if (!string.IsNullOrEmpty(item.MaterialPath))
                    {
                        string materialPath = ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.MaterialPath));
                        if (!Directory.Exists(materialPath)) throw new DirectoryNotFoundException($"Material path not found: {materialPath}");

                        await FileSystemService.CopyDirectoryAsync(materialPath, newItemPath, importParallelism);
                    }
                    
                    newItem.ItemPath = newItemPath;
                }

                ErrorOr<Success> result = await FileSystemService.CopyFileAsync(ItemUtils.GetItemPath(SystemPathV1.ItemThumbnailsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ImagePath)), Path.Combine(SystemPath.ItemThumbnailsPath, newItem.Id));
                if (!result.IsError) newItem.ThumbnailFileName = newItem.Id;
                else newItem.ThumbnailFileName = string.Empty;

                pathMapping[previousItemPath] = newItem.Id;

                items.Add(newItem);

                int percent = (int)(100.0 * i / v1Items.Count);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
                }
            }

            foreach (Item item in items)
            {
                IEnumerable<string> supportedAvatars = item.SupportedAvatarsView
                    .Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
                item.UpdateSupportedAvatars(supportedAvatars);

                IEnumerable<string> implementedAvatars = item.ImplementedAvatarsView
                    .Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
                item.UpdateImplementedAvatars(implementedAvatars);
            }

            List<CommonAvatar> commonAvatars = v1CommonAvatars.Select(CreateCommonAvatarFromCommonAvatarV1).ToList();

            foreach (CommonAvatar commonAvatar in commonAvatars)
            {
                IEnumerable<string> avatarPaths = commonAvatar.AvatarsView
                    .Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
                commonAvatar.UpdateAvatars(avatarPaths);
            }

            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));

            dataImportResult.Items.AddRange(items);
            dataImportResult.CommonAvatars.AddRange(commonAvatars);

            return dataImportResult;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data from v1.", ex);
            return Error.Failure(description: "Failed to import data from v1.");
        }
    }
    private static Item CreateItemFromItemV1(ItemV1 item)
    {
        Item migratedItem = new()
        {
            Title = item.Title,
            Author = item.AuthorName,
            AuthorId = item.AuthorId,
            BoothId = item.BoothId,
            ItemPath = item.ItemPath,
            ThumbnailFileName = MigrateAvatarExplorerV1Path(item.ImagePath),
            Type = item.Type,
            CustomCategory = item.CustomCategory,
            ItemMemo = item.ItemMemo,
            CreatedDate = item.CreatedDate,
            UpdatedDate = item.UpdatedDate
        };

        migratedItem.UpdateSupportedAvatars(item.SupportedAvatar);
        migratedItem.UpdateImplementedAvatars(item.ImplementedAvatars);
        migratedItem.UpdateTags(item.Tags);

        return migratedItem;
    }
    private static CommonAvatar CreateCommonAvatarFromCommonAvatarV1(CommonAvatarV1 commonAvatar)
    {
        CommonAvatar migratedCommonAvatar = new()
        {
            GroupName = commonAvatar.Name
        };

        migratedCommonAvatar.UpdateAvatars(commonAvatar.Avatars);

        return migratedCommonAvatar;
    }
    private static string MigrateAvatarExplorerV1Path(string path)
    {
        string migratedPath = path;

        // 古すぎるAEの場合は./が初めについていることがある
        if (path.StartsWith("./")) migratedPath = path[2..];

         // <sys>はフルパスとアプリフォルダの区別をつけるため
        if (migratedPath.StartsWith(V1ItemsFolderPrefix, StringComparison.Ordinal))
            return migratedPath.Replace(V1ItemsFolderPrefix, "<sys>");

        if (migratedPath.StartsWith(V1ThumbnailFolderPrefix, StringComparison.Ordinal))
            return migratedPath.Replace(V1ThumbnailFolderPrefix, "<sys>");

        return migratedPath;
    }

    private static async Task<ErrorOr<DataImportResult>> FromKonoAsset(string dataFolderPath, Dictionary<ItemType, string> localizedItemTypesMapping, bool copyAssetData, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            DataImportResult dataImportResult = new();
            int importParallelism = GetImportParallelism(runtimeSettings);

            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

            List<AbstractKonoAssetItem> konoAssetItems =
            [
                .. (FileSystemService.DeserializeClass<KonoAssetAvatarDatabase>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetWearableDatabase>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetWorldDatabase>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetOtherDatabase>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
            ];

            Dictionary<string, string> supportedAvatarMaps = new();
            foreach (string avatarName in konoAssetItems.OfType<KonoAssetWearableItem>().SelectMany(i => i.SupportedAvatars).Distinct())
            {
                if (supportedAvatarMaps.ContainsKey(avatarName)) continue;

                TempAvatar tempAvatar = new TempAvatar(avatarName);
                supportedAvatarMaps.Add(avatarName, tempAvatar.GetInternalId());
                dataImportResult.TempAvatars.Add(tempAvatar);
            }

            int lastPercent = -1;
            for (int i = 0; i < konoAssetItems.Count; i++)
            {
                AbstractKonoAssetItem konoAssetItem = konoAssetItems[i];
                Item item = konoAssetItem.ToItem();

                string newItemPath;
                if (copyAssetData)
                {
                    string safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
                    newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true);
                    
                    await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(KonoAssetPath.ItemsPath(dataFolderPath), item.ItemPath), newItemPath, importParallelism);
                }
                else
                {
                    newItemPath = ItemUtils.GetItemPath(KonoAssetPath.ItemsPath(dataFolderPath), item.ItemPath);
                    if (!Directory.Exists(newItemPath)) throw new DirectoryNotFoundException($"Item path not found: {newItemPath}");
                }

                item.ItemPath = newItemPath;

                if (!string.IsNullOrEmpty(konoAssetItem.Description.ImageFilename))
                {
                    ErrorOr<Success> result = await FileSystemService.CopyFileAsync(Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), konoAssetItem.Description.ImageFilename), Path.Combine(SystemPath.ItemThumbnailsPath, item.Id));
                    if (!result.IsError) item.ThumbnailFileName = item.Id;
                    else item.ThumbnailFileName = string.Empty;
                }

                item.UpdateSupportedAvatars(item.SupportedAvatarsView.Select(i => supportedAvatarMaps[i]));

                if (item.Type != ItemType.Avatar)
                {
                    bool categoryFoundFlag = false;
                    foreach (KeyValuePair<ItemType, string> itemTypeKpv in localizedItemTypesMapping)
                    {
                        if (item.CustomCategory == itemTypeKpv.Value)
                        {
                            item.Type = itemTypeKpv.Key;
                            item.CustomCategory = string.Empty;

                            categoryFoundFlag = true;
                            break;
                        }
                    }

                    if (!categoryFoundFlag) item.CustomCategory += " (From KonoAsset)";
                }

                dataImportResult.Items.Add(item);

                int percent = (int)(100.0 * i / konoAssetItems.Count);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
                }
            }

            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));

            return dataImportResult;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data from Asset.", ex);
            return Error.Failure("Failed to import data from KonoAsset.");
        }
    }

    private static async Task<ErrorOr<Success>> FromV1Thumbnail(IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

            // AEソフト本体のフォルダが渡された時はパスを変換して上げる
            if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName))) dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

            List<ItemV1> v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
            Dictionary<int, string> sourceThumbnailMap = new();

            foreach (ItemV1 sourceItem in v1Items)
            {
                if (sourceItem.BoothId == -1 || string.IsNullOrWhiteSpace(sourceItem.ImagePath)) continue;
                if (sourceThumbnailMap.ContainsKey(sourceItem.BoothId)) continue;

                string thumbnailPath = ItemUtils.GetItemPath(SystemPathV1.ItemThumbnailsPath(dataFolderPath), MigrateAvatarExplorerV1Path(sourceItem.ImagePath));
                if (File.Exists(thumbnailPath)) sourceThumbnailMap[sourceItem.BoothId] = thumbnailPath;
            }

            await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import thumbnails from v1.", ex);
            return Error.Failure(description: "Failed to import thumbnails from v1.");
        }
    }

    private static async Task<ErrorOr<Success>> FromKonoAssetThumbnail(IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

            List<AbstractKonoAssetItem> konoAssetItems =
            [
                .. (FileSystemService.DeserializeClass<KonoAssetAvatarDatabase>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetWearableDatabase>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetWorldDatabase>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
                .. (FileSystemService.DeserializeClass<KonoAssetOtherDatabase>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
            ];

            Dictionary<int, string> sourceThumbnailMap = new();
            foreach (AbstractKonoAssetItem sourceItem in konoAssetItems)
            {
                if (string.IsNullOrWhiteSpace(sourceItem.Description.ImageFilename)) continue;

                Item item = sourceItem.ToItem();
                if (item.BoothId == -1 || sourceThumbnailMap.ContainsKey(item.BoothId)) continue;

                string thumbnailPath = Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), sourceItem.Description.ImageFilename!);
                if (File.Exists(thumbnailPath)) sourceThumbnailMap[item.BoothId] = thumbnailPath;
            }

            await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to import thumbnails from KonoAsset.", ex);
            return Error.Failure(description: "Failed to import thumbnails from KonoAsset.");
        }
    }

    private static async Task ApplyThumbnailMap(IEnumerable<Item> currentItems, Dictionary<int, string> sourceThumbnailMap, Func<(string, int), Task>? reportProgress = null)
    {
        List<Item> targets = currentItems.Where(i => i.BoothId != -1).ToList();
        if (targets.Count == 0)
        {
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));
            return;
        }

        int lastPercent = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            Item targetItem = targets[i];
            if (sourceThumbnailMap.TryGetValue(targetItem.BoothId, out string? sourcePath))
            {
                ErrorOr<Success> copyResult = await FileSystemService.CopyFileAsync(sourcePath, Path.Combine(SystemPath.ItemThumbnailsPath, targetItem.Id));
                if (!copyResult.IsError) targetItem.ThumbnailFileName = targetItem.Id;
            }

            int percent = (int)(100.0 * (i + 1) / targets.Count);
            if (percent != lastPercent)
            {
                lastPercent = percent;
                if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
            }
        }
    }
}
