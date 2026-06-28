using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    #region Database
    public void LoadItemDatabase(string? path = null)
    {
        var loadPath = path ?? _itemDatabaseManager.DatabaseFilePath;
        ItemDatabaseMigrationService.Migrate(loadPath);
        _itemDatabaseManager.Load(loadPath);
        UpdateSearchIndex();
    }
    internal void EnsureAllItemsDefaultPathExist()
    {
        foreach (var item in _itemDatabaseManager.Items)
        {
            var path = ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath);
            if (!Directory.Exists(path) && !string.IsNullOrWhiteSpace(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception ex)
                {
                    ErrorManager.Instance.PostInternalError($"Failed to create directory for '{item.Id}'.", ex);
                }
            }
        }
    }

    public void LoadCommonAvatarDatabase(string? path = null) => _commonAvatarDatabaseManager.Load(path);
    public void LoadBulkImportPresetDatabase(string? path = null) => _bulkImportPresetDatabaseManager.Load(path);
    public void LoadTempAvatarsDatabase(string? path = null)
    {
        _tempAvatarsDatabaseManager.Load(path);
        UpdateSearchIndex();
    }

    public void SaveItemDatabase()
    {
        _itemDatabaseManager.Save();
        ItemDatabaseMigrationService.MarkCurrentVersion(_itemDatabaseManager.DatabaseFilePath);
    }
    public void SaveCommonAvatarDatabase() => _commonAvatarDatabaseManager.Save();
    public void SaveBulkImportPresetDatabase() => _bulkImportPresetDatabaseManager.Save();
    public void SaveTempAvatarsDatabase() => _tempAvatarsDatabaseManager.Save();

    public void ResetItemDatabase()
    {
        _itemDatabaseManager.Clear();
        SaveItemDatabase();
    }
    public void ResetCommonAvatarDatabase()
    {
        _commonAvatarDatabaseManager.Clear();
        SaveCommonAvatarDatabase();
    }
    public void ResetBulkImportPresetDatabase()
    {
        _bulkImportPresetDatabaseManager.Clear();
        SaveBulkImportPresetDatabase();
    }
    public void ResetTempAvatarDatabase()
    {
        _tempAvatarsDatabaseManager.Clear();
        SaveTempAvatarsDatabase();
    }
    #endregion

    #region Runtime Settings
    public void LoadRuntimeSettings(string? path = null) => _runtimeSettingsManager.Load(path);
    public void SaveRuntimeSettings() => _runtimeSettingsManager.Save();
    #endregion

    #region Update API
    public void UpdateSearchIndex(string? itemId = null)
    {
        var avatarTitleMaps = ItemUtils.GetItemTitleMaps(_itemDatabaseManager.Items.Where(i => i.Type == ItemType.Avatar), _tempAvatarsDatabaseManager.Items);

        if (itemId != null)
        {
            var item = GetItemById(itemId);
            if (item == null) return;

            _itemSearchIndexDictionary[item.Id] = ItemSearchService.BuildItemSearchIndex(item, avatarTitleMaps, _commonAvatarDatabaseManager.Items);
        }
        else
        {
            _itemSearchIndexDictionary.Clear();

            foreach (var item in _itemDatabaseManager.Items)
            {
                var index = ItemSearchService.BuildItemSearchIndex(item, avatarTitleMaps, _commonAvatarDatabaseManager.Items);
                _itemSearchIndexDictionary[item.Id] = index;
            }
        }
    }
    #endregion

    #region Resolve API
    public void ResolveTempAvatar(string tempAvatarId, string targetItemId)
    {
        foreach (var item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => i == tempAvatarId ? targetItemId : i).Distinct());
        }

        foreach (var commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.Avatars.Select(i => i == tempAvatarId ? targetItemId : i).Distinct());
        }

        SaveItemDatabase();
        SaveCommonAvatarDatabase();
        UpdateSearchIndex();

        RemoveTempAvatar(tempAvatarId);
    }
    #endregion
}
