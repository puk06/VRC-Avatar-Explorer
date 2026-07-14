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
    // private const string V1DatasFolderName = "Datas";
    // private static readonly string V1ItemsFolderPrefix = $"{V1DatasFolderName}\\Items\\";
    // private static readonly string V1ThumbnailFolderPrefix = $"{V1DatasFolderName}\\Thumbnail\\";

    // private static int GetImportParallelism(RuntimeSettings runtimeSettings)
    // {
    //     int requested = runtimeSettings.MaxDegreeOfParallelism;
    //     int cappedByCpu = Math.Max(1, Environment.ProcessorCount - 1);
    //     return Math.Clamp(requested - 1, 1, cappedByCpu);
    // }

    // internal static async Task<ErrorOr<DataImportResult>> Import(ImportRequest importRequest)
    // {
    //     return importRequest.ImportType switch
    //     {
    //         DataImportType.V1 => await FromV1(importRequest),
    //         DataImportType.KonoAsset => await FromKonoAsset(importRequest),
    //         _ => Error.Unexpected(description: $"Unexpected import type: {importRequest.ImportType}")
    //     };
    // }

    // internal static async Task<ErrorOr<Success>> ImportThumbnail(ThumbnailImportType importType, IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    // {
    //     return importType switch
    //     {
    //         ThumbnailImportType.V1 => await FromV1Thumbnail(currentItems, dataFolderPath, reportProgress),
    //         ThumbnailImportType.KonoAsset => await FromKonoAssetThumbnail(currentItems, dataFolderPath, reportProgress),
    //         _ => Error.Unexpected(description: $"Unexpected thumbnail import type: {importType}")
    //     };
    // }
    
    // private static async Task<ErrorOr<DataImportResult>> FromV1(ImportRequest importRequest)
    // {
    //     try
    //     {
    //         var dataFolderPath = importRequest.DataFolderPath;
    //         var copyAssetData = importRequest.CopyAssetData;
    //         var runtimeSettings = importRequest.RuntimeSettings;
    //         var reportProgress = importRequest.ReportProgress;

    //         var dataImportResult = new DataImportResult();
    //         var importParallelism = GetImportParallelism(runtimeSettings);
        
    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

    //         // AEソフト本体のフォルダが渡された時はパスを変換して上げる
    //         if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName))) dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

    //         var v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
    //         var v1CommonAvatars = FileSystemService.DeserializeClass<List<CommonAvatarV1>>(SystemPathV1.CommonAvatarDatabasePath(dataFolderPath)).Value ?? [];

    //         var items = new List<Item>();

    //         var pathMapping = new Dictionary<string, string>();

    //         // データ移行処理
    //         int lastPercent = -1;
    //         for (int i = 0; i < v1Items.Count; i++)
    //         {
    //             var item = v1Items[i];
    //             var previousItemPath = item.ItemPath;
                
    //             var newItem = CreateItemFromItemV1(item);

    //             if (copyAssetData)
    //             {
    //                 var safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
    //                 var newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true) ?? throw new DirectoryNotFoundException("Counldn't get unique item path");
                    
    //                 await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ItemPath)), newItemPath, importParallelism);
    //                 if (!string.IsNullOrEmpty(item.MaterialPath)) await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.MaterialPath)), newItemPath, importParallelism);
                
    //                 newItem.ItemPath = $"<sys>{Path.GetRelativePath(runtimeSettings.DataRootDirectory, newItemPath)}";
    //             }
    //             else
    //             {
    //                 var newItemPath = ItemUtils.GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ItemPath));
    //                 var materialPath = ItemUtils.GetItemPath(SystemPathV1.ItemsFolderPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.MaterialPath));
                    
    //                 newItem.ItemPath = newItemPath;
    //                 if (!string.IsNullOrEmpty(item.MaterialPath)) newItem.UpdateItemPaths([materialPath]);
    //             }

    //             var result = await FileSystemService.CopyFileAsync(ItemUtils.GetItemPath(SystemPathV1.ItemThumbnailsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ImagePath)), Path.Combine(SystemPath.ItemThumbnailsFolderPath, newItem.Id));
    //             if (!result.IsError) newItem.ThumbnailFileName = newItem.Id;
    //             else newItem.ThumbnailFileName = string.Empty;

    //             pathMapping[previousItemPath] = newItem.Id;

    //             items.Add(newItem);

    //             int percent = (int)(100.0 * i / v1Items.Count);
    //             if (percent != lastPercent)
    //             {
    //                 lastPercent = percent;
    //                 if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
    //             }
    //         }

    //         foreach (Item item in items)
    //         {
    //             var supportedAvatars = item.SupportedAvatars.Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
    //             item.UpdateSupportedAvatars(supportedAvatars);

    //             var implementedAvatars = item.ImplementedAvatars.Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
    //             item.UpdateImplementedAvatars(implementedAvatars);
    //         }

    //         var commonAvatars = v1CommonAvatars.Select(CreateCommonAvatarFromCommonAvatarV1).ToList();
    //         foreach (var commonAvatar in commonAvatars)
    //         {
    //             var avatarPaths = commonAvatar.Avatars.Select(a => pathMapping.TryGetValue(a, out string? value) ? value : a);
    //             commonAvatar.UpdateAvatars(avatarPaths);
    //         }

    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));

    //         dataImportResult.Items.AddRange(items);
    //         dataImportResult.CommonAvatars.AddRange(commonAvatars);

    //         return dataImportResult;
    //     }
    //     catch (Exception ex)
    //     {
    //         ErrorManager.Instance.PostInternalError("Failed to import data from v1.", ex);
    //         return Error.Failure(description: "Failed to import data from v1.");
    //     }
    // }
    // private static Item CreateItemFromItemV1(ItemV1 item)
    // {
    //     var migratedItem = new Item()
    //     {
    //         Title = item.Title,
    //         Author = item.AuthorName,
    //         AuthorId = item.AuthorId,
    //         BoothId = item.BoothId,
    //         ItemPath = item.ItemPath,
    //         ThumbnailFileName = MigrateAvatarExplorerV1Path(item.ImagePath),
    //         Type = (ItemType)(item.Type + 1),
    //         CustomCategory = item.CustomCategory,
    //         ItemMemo = item.ItemMemo,
    //         CreatedDate = item.CreatedDate,
    //         UpdatedDate = item.UpdatedDate
    //     };

    //     migratedItem.UpdateSupportedAvatars(item.SupportedAvatar);
    //     migratedItem.UpdateImplementedAvatars(item.ImplementedAvatars);
    //     migratedItem.UpdateTags(item.Tags);

    //     return migratedItem;
    // }
    // private static CommonAvatar CreateCommonAvatarFromCommonAvatarV1(CommonAvatarV1 commonAvatar)
    // {
    //     var migratedCommonAvatar = new CommonAvatar()
    //     {
    //         GroupName = commonAvatar.Name
    //     };

    //     migratedCommonAvatar.UpdateAvatars(commonAvatar.Avatars);

    //     return migratedCommonAvatar;
    // }
    // private static string MigrateAvatarExplorerV1Path(string path)
    // {
    //     var migratedPath = path;

    //     // 古すぎるAEの場合は./が初めについていることがある
    //     if (path.StartsWith("./")) migratedPath = path[2..];

    //      // <sys>はフルパスとアプリフォルダの区別をつけるため
    //     if (migratedPath.StartsWith(V1ItemsFolderPrefix, StringComparison.Ordinal))
    //         return migratedPath.Replace(V1ItemsFolderPrefix, "<sys>");

    //     if (migratedPath.StartsWith(V1ThumbnailFolderPrefix, StringComparison.Ordinal))
    //         return migratedPath.Replace(V1ThumbnailFolderPrefix, "<sys>");

    //     return migratedPath;
    // }

    // private static async Task<ErrorOr<DataImportResult>> FromKonoAsset(ImportRequest importRequest)
    // {
    //     try
    //     {
    //         var dataFolderPath = importRequest.DataFolderPath;
    //         var copyAssetData = importRequest.CopyAssetData;
    //         var runtimeSettings = importRequest.RuntimeSettings;
    //         var reportProgress = importRequest.ReportProgress;

    //         var dataImportResult = new DataImportResult();
    //         var importParallelism = GetImportParallelism(runtimeSettings);

    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

    //         List<AbstractKonoAssetItem> konoAssetItems =
    //         [
    //             .. (FileSystemService.DeserializeClass<KonoAssetAvatarDatabase>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetWearableDatabase>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetWorldDatabase>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetOtherDatabase>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //         ];

    //         var supportedAvatarMaps = new Dictionary<string, string>();
    //         foreach (var avatarName in konoAssetItems.OfType<KonoAssetWearableItem>().SelectMany(i => i.SupportedAvatars).Distinct())
    //         {
    //             if (supportedAvatarMaps.ContainsKey(avatarName)) continue;

    //             var tempAvatar = new TempAvatar(avatarName);
    //             supportedAvatarMaps.Add(avatarName, tempAvatar.GetInternalId());
    //             dataImportResult.TempAvatars.Add(tempAvatar);
    //         }

    //         int lastPercent = -1;
    //         for (int i = 0; i < konoAssetItems.Count; i++)
    //         {
    //             var konoAssetItem = konoAssetItems[i];
    //             var item = konoAssetItem.ToItem();

    //             string newItemPath;
    //             if (copyAssetData)
    //             {
    //                 var safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
    //                 newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true);
                    
    //                 await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(KonoAssetPath.DataPath(dataFolderPath), item.ItemPath), newItemPath, importParallelism);
    //             }
    //             else
    //             {
    //                 newItemPath = ItemUtils.GetItemPath(KonoAssetPath.DataPath(dataFolderPath), item.ItemPath);
    //             }

    //             item.ItemPath = newItemPath;

    //             if (!string.IsNullOrEmpty(konoAssetItem.Description.ImageFilename))
    //             {
    //                 ErrorOr<Success> result = await FileSystemService.CopyFileAsync(Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), konoAssetItem.Description.ImageFilename), Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id));
    //                 if (!result.IsError) item.ThumbnailFileName = item.Id;
    //                 else item.ThumbnailFileName = string.Empty;
    //             }

    //             item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => supportedAvatarMaps[i]));

    //             if (item.Type != ItemType.Avatar)
    //             {
    //                 var categoryFoundFlag = false;
    //                 foreach (var itemTypeKpv in importRequest.LocalizedItemTypesMapping)
    //                 {
    //                     if (item.CustomCategory == itemTypeKpv.Value)
    //                     {
    //                         item.Type = itemTypeKpv.Key;
    //                         item.CustomCategory = string.Empty;

    //                         categoryFoundFlag = true;
    //                         break;
    //                     }
    //                 }

    //                 if (!categoryFoundFlag) item.CustomCategory += " (From KonoAsset)";
    //             }

    //             dataImportResult.Items.Add(item);

    //             int percent = (int)(100.0 * i / konoAssetItems.Count);
    //             if (percent != lastPercent)
    //             {
    //                 lastPercent = percent;
    //                 if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
    //             }
    //         }

    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));

    //         return dataImportResult;
    //     }
    //     catch (Exception ex)
    //     {
    //         ErrorManager.Instance.PostInternalError("Failed to import data from Asset.", ex);
    //         return Error.Failure(description: "Failed to import data from KonoAsset.");
    //     }
    // }

    // private static async Task<ErrorOr<Success>> FromV1Thumbnail(IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    // {
    //     try
    //     {
    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

    //         // AEソフト本体のフォルダが渡された時はパスを変換して上げる
    //         if (Directory.Exists(Path.Combine(dataFolderPath, V1DatasFolderName))) dataFolderPath = Path.Combine(dataFolderPath, V1DatasFolderName);

    //         var v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
    //         var sourceThumbnailMap = new Dictionary<int, string>();

    //         foreach (var sourceItem in v1Items)
    //         {
    //             if (sourceItem.BoothId == -1 || string.IsNullOrWhiteSpace(sourceItem.ImagePath)) continue;
    //             if (sourceThumbnailMap.ContainsKey(sourceItem.BoothId)) continue;

    //             var thumbnailPath = ItemUtils.GetItemPath(SystemPathV1.ItemThumbnailsPath(dataFolderPath), MigrateAvatarExplorerV1Path(sourceItem.ImagePath));
    //             if (File.Exists(thumbnailPath)) sourceThumbnailMap[sourceItem.BoothId] = thumbnailPath;
    //         }

    //         await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);

    //         return Result.Success;
    //     }
    //     catch (Exception ex)
    //     {
    //         ErrorManager.Instance.PostInternalError("Failed to import thumbnails from v1.", ex);
    //         return Error.Failure(description: "Failed to import thumbnails from v1.");
    //     }
    // }

    // private static async Task<ErrorOr<Success>> FromKonoAssetThumbnail(IEnumerable<Item> currentItems, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    // {
    //     try
    //     {
    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

    //         List<AbstractKonoAssetItem> konoAssetItems =
    //         [
    //             .. (FileSystemService.DeserializeClass<KonoAssetAvatarDatabase>(KonoAssetPath.AvatarsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetWearableDatabase>(KonoAssetPath.AvatarWearablesDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetWorldDatabase>(KonoAssetPath.WorldObjectsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //             .. (FileSystemService.DeserializeClass<KonoAssetOtherDatabase>(KonoAssetPath.OtherAssetsDatabasePath(dataFolderPath)).Value ?? new()).Data,
    //         ];

    //         var sourceThumbnailMap = new Dictionary<int, string>();
    //         foreach (var sourceItem in konoAssetItems)
    //         {
    //             if (string.IsNullOrWhiteSpace(sourceItem.Description.ImageFilename)) continue;

    //             var item = sourceItem.ToItem();
    //             if (item.BoothId == -1 || sourceThumbnailMap.ContainsKey(item.BoothId)) continue;

    //             var thumbnailPath = Path.Combine(KonoAssetPath.ThumbnailsPath(dataFolderPath), sourceItem.Description.ImageFilename!);
    //             if (File.Exists(thumbnailPath)) sourceThumbnailMap[item.BoothId] = thumbnailPath;
    //         }

    //         await ApplyThumbnailMap(currentItems, sourceThumbnailMap, reportProgress);

    //         return Result.Success;
    //     }
    //     catch (Exception ex)
    //     {
    //         ErrorManager.Instance.PostInternalError("Failed to import thumbnails from KonoAsset.", ex);
    //         return Error.Failure(description: "Failed to import thumbnails from KonoAsset.");
    //     }
    // }

    // private static async Task ApplyThumbnailMap(IEnumerable<Item> currentItems, Dictionary<int, string> sourceThumbnailMap, Func<(string, int), Task>? reportProgress = null)
    // {
    //     var targets = currentItems.Where(i => i.BoothId != -1).ToArray();
    //     if (targets.Length == 0)
    //     {
    //         if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 100));
    //         return;
    //     }

    //     var lastPercent = -1;
    //     for (int i = 0; i < targets.Length; i++)
    //     {
    //         var targetItem = targets[i];
    //         if (sourceThumbnailMap.TryGetValue(targetItem.BoothId, out string? sourcePath))
    //         {
    //             var copyResult = await FileSystemService.CopyFileAsync(sourcePath, Path.Combine(SystemPath.ItemThumbnailsFolderPath, targetItem.Id));
    //             if (!copyResult.IsError) targetItem.ThumbnailFileName = targetItem.Id;
    //         }

    //         var percent = (int)(100.0 * (i + 1) / targets.Length);
    //         if (percent != lastPercent)
    //         {
    //             lastPercent = percent;
    //             if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, percent));
    //         }
    //     }
    // }
}
