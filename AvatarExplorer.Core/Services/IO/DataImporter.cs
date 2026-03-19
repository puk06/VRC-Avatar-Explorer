using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Data.Paths.External.KonoAsset;
using AvatarExplorer.Core.Data.Paths.External.V1;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.External.KonoAsset.Databases;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;
using AvatarExplorer.Core.Models.External.V1;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

internal static class DataImporter
{
    internal static async Task<ErrorOr<DataImportResult>> Import(DataImportType importType, string dataFolderPath, Dictionary<ItemType, string> localizedItemTypesMapping, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        return importType switch
        {
            DataImportType.V1 => await FromV1(dataFolderPath, runtimeSettings, reportProgress),
            DataImportType.KonoAsset => await FromKonoAsset(dataFolderPath, localizedItemTypesMapping, runtimeSettings, reportProgress),
            _ => Error.Unexpected(description: $"Unexpected import type: {importType}")
        };
    }
    
    private static async Task<ErrorOr<DataImportResult>> FromV1(string dataFolderPath, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            DataImportResult dataImportResult = new();
        
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 0));

            // AEソフト本体のフォルダが渡された時はパスを変換して上げる
            if (Directory.Exists(Path.Combine(dataFolderPath, "Datas"))) dataFolderPath = Path.Combine(dataFolderPath, "Datas");

            List<ItemV1> v1Items = FileSystemService.DeserializeClass<List<ItemV1>>(SystemPathV1.ItemDatabasePath(dataFolderPath)).Value ?? [];
            List<CommonAvatarV1> v1CommonAvatars = FileSystemService.DeserializeClass<List<CommonAvatarV1>>(SystemPathV1.CommonAvatarDatabasePath(dataFolderPath)).Value ?? [];

            // １個１個チェックしながらコピーしても良いかも
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 10));
            await FileSystemService.CopyDirectoryAsync(SystemPathV1.AuthorThumbnailsPath(dataFolderPath), SystemPath.AuthorThumbnailsPath, runtimeSettings.MaxDegreeOfParallelism);

            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Import.Copying, 20));
            await FileSystemService.CopyDirectoryAsync(SystemPathV1.ItemThumbnailsPath(dataFolderPath), SystemPath.ItemThumbnailsPath, runtimeSettings.MaxDegreeOfParallelism);

            List<Item> items = new();

            Dictionary<string, string> pathMapping = new();

            // データ移行処理
            int lastPercent = -1;
            for (int i = 0; i < v1Items.Count; i++)
            {
                ItemV1 item = v1Items[i];
                string previousItemPath = item.ItemPath;

                string safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
                string newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true) ?? throw new DirectoryNotFoundException("Counldn't get unique item path");
                
                await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.ItemPath)), newItemPath, runtimeSettings.MaxDegreeOfParallelism);
                if (!string.IsNullOrEmpty(item.MaterialPath)) await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(SystemPathV1.ItemsPath(dataFolderPath), MigrateAvatarExplorerV1Path(item.MaterialPath)), newItemPath, runtimeSettings.MaxDegreeOfParallelism);
                
                Item newItem = CreateItemFromItemV1(item);
                newItem.ItemPath = $"<sys>{Path.GetRelativePath(runtimeSettings.DataRootDirectory, newItemPath)}";

                pathMapping[previousItemPath] = newItem.Id;

                items.Add(newItem);

                int percent = 20 + (int)(80.0 * i / v1Items.Count);
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
            ThumbnmailFileName = MigrateAvatarExplorerV1Path(item.ImagePath),
            AuthorThumbnmailFileName = MigrateAvatarExplorerV1Path(item.AuthorImageFilePath),
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
        const string V1ItemsFolderPrefix = "Datas\\Items\\";
        const string V1ThumbnailFolderPrefix = "Datas\\Thumbnail\\";
        const string V1AuthorThumbnailFolderPrefix = "Datas\\AuthorImage\\";

        if (path.StartsWith(V1ItemsFolderPrefix))
            return path.Replace(V1ItemsFolderPrefix, "<sys>"); // フルパスとアプリフォルダの区別をつけるため

        if (path.StartsWith(V1ThumbnailFolderPrefix))
            return path.Replace(V1ThumbnailFolderPrefix, string.Empty);

        if (path.StartsWith(V1AuthorThumbnailFolderPrefix))
            return path.Replace(V1AuthorThumbnailFolderPrefix, string.Empty);

        return path;
    }

    private static async Task<ErrorOr<DataImportResult>> FromKonoAsset(string dataFolderPath, Dictionary<ItemType, string> localizedItemTypesMapping, RuntimeSettings runtimeSettings, Func<(string, int), Task>? reportProgress = null)
    {
        try
        {
            DataImportResult dataImportResult = new();

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
                Item item = konoAssetItems[i].ToItem();

                string safeItemTitle = ItemUtils.GetSafeTitle(item.Title) ?? Path.GetFileNameWithoutExtension(item.ItemPath);
                string newItemPath = FileSystemService.GetUniquePath(runtimeSettings.DataRootDirectory, safeItemTitle, isDirectory: true);

                await FileSystemService.CopyDirectoryAsync(ItemUtils.GetItemPath(KonoAssetPath.ItemsPath(dataFolderPath), item.ItemPath), newItemPath, runtimeSettings.MaxDegreeOfParallelism);
                item.ItemPath = newItemPath;

                if (item.BoothId != -1)
                {
                    ErrorOr<BoothItem> fetchResult = await BoothService.GetItem(item.BoothId.ToString());

                    if (!fetchResult.IsError)
                    {
                        item.AuthorId = fetchResult.Value.AuthorId; // IKonoAssetItem.ToItem()ではAuthorIdは移行されないためここで設定する必要がある。

                        string itemThumbnailFileName = item.BoothId + ".png";
                        bool itemThumbnailResult = await ImageDownloader.Fetch(fetchResult.Value.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsPath, itemThumbnailFileName), false);
                        if (itemThumbnailResult) item.ThumbnmailFileName = itemThumbnailFileName;

                        string authorThumbnailFileName = item.AuthorId + ".png";
                        bool authorThumbnailResult = await ImageDownloader.Fetch(fetchResult.Value.Shop.ThumbnailUrl, Path.Combine(SystemPath.AuthorThumbnailsPath, authorThumbnailFileName), false);
                        if (authorThumbnailResult) item.AuthorThumbnmailFileName = authorThumbnailFileName;

                        await Task.Delay(750 * 3);
                    }
                    else
                    {
                        await Task.Delay(750);
                    }
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
}
