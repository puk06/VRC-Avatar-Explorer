using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    #region Set API
    public void SetRuntimeSettings(RuntimeSettings runtimeSettings)
    {
        _runtimeSettingsManager.Update(runtimeSettings);

        _backupManager.SetAutoBackupPath(RuntimeSettings.AutoBackupRootDirectory);
        _backupManager.SetAutoBackupInterval(RuntimeSettings.AutoBackupInterval);
    }
    #endregion

    #region Add API
    public string AddCommonAvatar(string groupName, IEnumerable<string>? avatars = null)
    {
        CommonAvatar commonAvatar = new()
        {
            GroupName = groupName
        };

        if (avatars != null)
        {
            commonAvatar.UpdateAvatars(avatars);
        }

        _commonAvatarDatabaseManager.Add(commonAvatar);

        UpdateSearchIndex();

        SaveCommonAvatarDatabase();

        return commonAvatar.Id;
    }
    public string AddTempAvatar(string avatarName)
    {
        var tempAvatar = new TempAvatar(avatarName);

        _tempAvatarsDatabaseManager.Add(tempAvatar);

        UpdateSearchIndex();

        SaveTempAvatarsDatabase();

        return tempAvatar.Id;
    }
    public string AddBulkImportPreset(string presetName, IEnumerable<BulkImportItem>? items = null)
    {
        var bulkImportPreset = new BulkImportPreset()
        {
            PresetName = presetName
        };

        if (items != null)
        {
            bulkImportPreset.UpdateItems(items);
        }

        _bulkImportPresetDatabaseManager.Add(bulkImportPreset);

        SaveBulkImportPresetDatabase();

        return bulkImportPreset.Id;
    }
    public async Task<ErrorOr<ItemCreationResult>> AddItem(ItemCreationContext itemCreationContext)
    {
        var itemCreationResult = await ItemCreator.FromItemCreationContext(itemCreationContext, RuntimeSettings);
        if (itemCreationResult.IsError) return Error.Failure(description: itemCreationResult.Errors.ToErrorString());

        if (itemCreationResult.Value.Item == null) return itemCreationResult;

        var currentUnixTime = DatetimeUtils.GetCurrentUnixTime();
        itemCreationResult.Value.Item.CreatedDate = currentUnixTime;
        itemCreationResult.Value.Item.UpdatedDate = currentUnixTime;

        _itemDatabaseManager.Add(itemCreationResult.Value.Item);
        UpdateSearchIndex(itemCreationResult.Value.Item.Id);

        SaveItemDatabase();

        return itemCreationResult;
    }
    public async Task<ErrorOr<ExtractResult>> AddItemPaths(string itemId, string[] paths)
    {
        var item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        var extractResult = await FileSystemService.ExtractItemPaths(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath), paths, RuntimeSettings);
        if (extractResult.IsError) return Error.Failure(description: extractResult.Errors.ToErrorString());

        item.UpdateItemPaths(extractResult.Value.FolderPaths); // ItemPathは更新しない（既に設定されているため）

        UpdateItemUpdatedDate(itemId);
        SaveItemDatabase();

        return extractResult;
    }
    #endregion

    #region Edit API
    public async Task<bool> EditItem(string itemId, ItemCreationContext itemCreationContext)
    {
        var item = GetItemById(itemId);
        if (item == null) return false;

        item.SetValuesFromCreationContext(itemCreationContext);

        var addItemPathsResult = await AddItemPaths(item.Id, itemCreationContext.ItemPaths.ToArray());
        if (addItemPathsResult.IsError) return false;
        if (addItemPathsResult.Value.ProcessingFailedPaths.Count > 0) return false;

        UpdateItemUpdatedDate(itemId);
        UpdateSearchIndex();

        SaveItemDatabase();

        return true;
    }

    public void EditCustomCategoryName(string previousName, string newName)
    {
        foreach (var item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Custom && i.CustomCategory == previousName))
        {
            item.CustomCategory = newName;
        }

        UpdateSearchIndex();

        SaveItemDatabase();
    }
    #endregion

    #region Update API
    public void UpdateItemUpdatedDate(string id)
    {
        var item = GetItemById(id);
        if (item == null) return;

        item.UpdatedDate = DatetimeUtils.GetCurrentUnixTime();
    }
    public void ChangeItemPath(string id, string path)
    {
        var item = GetItemById(id);
        if (item == null) return;

        var newPath = path;

        if (path.StartsWith(RuntimeSettings.DataRootDirectory))
            newPath = $"<sys>{Path.GetRelativePath(RuntimeSettings.DataRootDirectory, path)}";

        item.ItemPath = newPath;
    }
    #endregion

    #region Update Thumbnail API
    public async Task<ErrorOr<Success>> UpdateItemThumbnail(string itemId, string imageFilePath)
    {
        var item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        var result = await FileSystemService.CopyFileAsync(imageFilePath, Path.Combine(SystemPath.ItemThumbnailsFolderPath, Path.GetFileName(imageFilePath)));
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        item.ThumbnailFileName = Path.GetFileName(imageFilePath);
        UpdateItemUpdatedDate(itemId);

        SaveItemDatabase();

        return Result.Success;
    }
    #endregion

    #region Replace API
    public void ReplaceCommonAvatarGroupToSupportedAvatars(string groupId)
    {
        var commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        var internalId = commonAvatar.GetInternalId();

        foreach (var item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.SelectMany(i => i == internalId ? commonAvatar.Avatars : [i]).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupId)
    {
        var commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        var internalId = commonAvatar.GetInternalId();

        foreach (var item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => commonAvatar.Avatars.Contains(i) ? internalId : i).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }

    public void ConvertDatabaseRelativePathsToFullPaths(string previousDataRootDirectory)
    {
        foreach (var item in _itemDatabaseManager.Items)
        {
            var currentPath = ItemUtils.GetItemPath(previousDataRootDirectory, item.ItemPath);
            item.ItemPath = currentPath;
        }

        SaveItemDatabase();
    }
    #endregion

    #region Remove API
    public bool RemoveItem(string id, bool removeAssetData = false)
    {
        if (removeAssetData)
        {
            var item = GetItemById(id);
            if (item != null) FileSystemService.DeleteDirectory(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
        }

        bool removed = _itemDatabaseManager.Remove(id);

        foreach (var item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Where(i => i != id));
            item.UpdateImplementedAvatars(item.ImplementedAvatars.Where(i => i != id));
        }

        foreach (var commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.Avatars.Where(i => i != id));
        }

        UpdateSearchIndex();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return removed;
    }

    public bool RemoveCommonAvatar(string internalId)
    {
        var id = CommonAvatar.GetGroupId(internalId);
        if (id == null) return false;

        bool removed = _commonAvatarDatabaseManager.Remove(id);

        foreach (var item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatars.Where(i => i != internalId));
        }

        UpdateSearchIndex();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return removed;
    }

    public bool RemoveBulkImportPreset(string id)
    {
        bool removed = _bulkImportPresetDatabaseManager.Remove(id);
        SaveBulkImportPresetDatabase();

        return removed;
    }

    public bool RemoveTempAvatar(string internalId)
    {
        var id = TempAvatar.GetAvatarId(internalId);
        if (id == null) return false;

        bool removed = _tempAvatarsDatabaseManager.Remove(id);

        foreach (var item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatars.Where(i => i != internalId));
        }

        foreach (var commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.Avatars.Where(i => i != internalId));
        }

        UpdateSearchIndex();
        SaveTempAvatarsDatabase();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return removed;
    }
    #endregion

    #region Merge API
    public void MergeItemCategories(ItemCategory sourceCategory, ItemCategory targetCategory)
    {
        foreach (var item in _itemDatabaseManager.Items.Where(i => i.IsCategoryMatch(sourceCategory.CategoryName)))
        {
            item.Type = targetCategory.Type;
            item.CustomCategory = targetCategory.CustomCategory;
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }
    #endregion
}
