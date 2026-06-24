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
        TempAvatar tempAvatar = new TempAvatar(avatarName);

        _tempAvatarsDatabaseManager.Add(tempAvatar);

        UpdateSearchIndex();

        SaveTempAvatarsDatabase();

        return tempAvatar.Id;
    }
    public string AddBulkImportPreset(string presetName, IEnumerable<BulkImportItem>? items = null)
    {
        BulkImportPreset bulkImportPreset = new()
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
        ErrorOr<ItemCreationResult> itemCreationResult = await ItemCreator.FromItemCreationContext(itemCreationContext, RuntimeSettings);
        if (itemCreationResult.IsError) return Error.Failure(description: itemCreationResult.Errors.ToErrorString());

        if (itemCreationResult.Value.Item == null) return itemCreationResult;

        string currentUnixTime = DatetimeUtils.GetCurrentUnixTime();
        itemCreationResult.Value.Item.CreatedDate = currentUnixTime;
        itemCreationResult.Value.Item.UpdatedDate = currentUnixTime;

        _itemDatabaseManager.Add(itemCreationResult.Value.Item);
        UpdateSearchIndex(itemCreationResult.Value.Item.Id);

        SaveItemDatabase();

        return itemCreationResult;
    }
    public async Task<ErrorOr<ExtractResult>> AddItemPaths(string itemId, string[] paths)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        ErrorOr<ExtractResult> extractResult = await FileSystemService.ExtractItemPaths(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath), paths, RuntimeSettings);
        if (extractResult.IsError) return Error.Failure(description: extractResult.Errors.ToErrorString());

        UpdateItemUpdatedDate(itemId);
        SaveItemDatabase();

        return extractResult;
    }
    #endregion

    #region Edit API
    public async Task<bool> EditItem(string itemId, ItemCreationContext itemCreationContext)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return false;

        item.SetValuesFromCreationContext(itemCreationContext);

        ErrorOr<ExtractResult> addItemPathsResult = await AddItemPaths(item.Id, itemCreationContext.ItemPaths.ToArray());
        if (addItemPathsResult.IsError) return false;
        if (addItemPathsResult.Value.ProcessingFailedPaths.Count > 0) return false;

        UpdateItemUpdatedDate(itemId);
        UpdateSearchIndex();

        SaveItemDatabase();

        return true;
    }

    public void EditCustomCategoryName(string previousName, string newName)
    {
        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Custom && i.CustomCategory == previousName))
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
        Item? item = GetItemById(id);
        if (item == null) return;

        item.UpdatedDate = DatetimeUtils.GetCurrentUnixTime();
    }
    #endregion

    #region Update Thumbnail API
    public async Task<ErrorOr<Success>> UpdateItemThumbnail(string itemId, string imageFilePath)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        ErrorOr<Success> result = await FileSystemService.CopyFileAsync(imageFilePath, Path.Combine(SystemPath.ItemThumbnailsFolderPath, Path.GetFileName(imageFilePath)));
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
        CommonAvatar? commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        string internalId = commonAvatar.GetInternalId();

        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.SelectMany(i => i == internalId ? commonAvatar.AvatarsView : [i]).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupId)
    {
        CommonAvatar? commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        string internalId = commonAvatar.GetInternalId();

        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Select(i => commonAvatar.AvatarsView.Contains(i) ? internalId : i).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }

    public void ConvertDatabaseRelativePathsToFullPaths(string previousDataRootDirectory)
    {
        foreach (Item item in _itemDatabaseManager.Items)
        {
            string currentPath = ItemUtils.GetItemPath(previousDataRootDirectory, item.ItemPath);
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
            Item? item = GetItemById(id);
            if (item != null) FileSystemService.DeleteDirectory(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
        }

        bool removed = _itemDatabaseManager.Remove(id);

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != id));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != id));
        }

        foreach (CommonAvatar commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Where(i => i != id));
        }

        UpdateSearchIndex();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return removed;
    }

    public bool RemoveCommonAvatar(string internalId)
    {
        string? id = CommonAvatar.GetGroupId(internalId);
        if (id == null) return false;

        bool removed = _commonAvatarDatabaseManager.Remove(id);

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != internalId));
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
        string? id = TempAvatar.GetAvatarId(internalId);
        if (id == null) return false;

        bool removed = _tempAvatarsDatabaseManager.Remove(id);

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != internalId));
        }

        foreach (CommonAvatar commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Where(i => i != internalId));
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
        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.IsCategoryMatch(sourceCategory.CategoryName)))
        {
            item.Type = targetCategory.Type;
            item.CustomCategory = targetCategory.CustomCategory;
        }

        UpdateSearchIndex();

        SaveItemDatabase();
    }
    #endregion
}
