using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System.Repositories;

namespace AvatarExplorer.Core.Services.System;

public class AvatarExplorerApp
{
    public static readonly string CurrentVersion = "2.7.0-beta.6";

    private static readonly AvatarExplorerApp _instance = new();
    public static AvatarExplorerApp Instance => _instance;

    private bool _initialized = false;

    public ItemRepository Items { get; } = new();
    public CommonAvatarRepository CommonAvatars { get; } = new();
    public TempAvatarRepository TempAvatars { get; } = new();
    public BulkImportPresetRepository BulkImportPresets { get; } = new();
    public ItemGroupService ItemGroupService { get; }
    public ItemNavigationService ItemNavigationService { get; }
    public RuntimeSettingsRepository RuntimeSettings { get; } = new();

    public Func<ArchivePasswordRequest, ValueTask<string?>>? ArchivePasswordProvider { get; set; }

    public readonly BackupManager BackupManager = new();

    private AvatarExplorerApp()
    {
        ItemGroupService = new(Items, CommonAvatars, TempAvatars, RuntimeSettings);
        ItemNavigationService = new(ItemGroupService);
    }

    public void Initialize()
    {
        if (_initialized) return;

        Items.Load();
        CommonAvatars.Load();
        TempAvatars.Load();
        BulkImportPresets.Load();
        RuntimeSettings.Load();

        Migration(); //TODO: ItemMigrationに移行する

        ItemGroupService.RebuildIndices();

        BackupManager.AddTargetFiles(
            [
                SystemPath.ItemDatabasePath,
                SystemPath.ItemDatabaseMigrationVersionPath,
                SystemPath.CommonAvatarDatabasePath,
                SystemPath.TempAvatarsDatabasePath,
                SystemPath.BulkImportPresetDatabasePath,
                SystemPath.RuntimeSettingsFilePath,
            ]
        );

        // TODO: StartAutoBackup();

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;

        _initialized = true;
    }

    private void Migration()
    {
        // Item
        var items = Items.GetAll();
        items.ForEach(i =>
        {
            var migratedPath = i.ItemPath.StartsWith("<sys>") ? Path.Join(RuntimeSettings.Settings.DataRootDirectory, i.ItemPath.Replace("<sys>", string.Empty)) : i.ItemPath;
            i.UpdateItemPath(migratedPath);

            i.UpdateSupportedAvatars(i.SupportedAvatars.Select(i =>
            {
                if (i.StartsWith("<sys:temp>"))
                {
                    return i.Replace("<sys:temp>", "tempavatar:");
                }
                else if (i.StartsWith("<sys:commonavatar>"))
                {
                    return i.Replace("<sys:commonavatar>", "commonavatar:");
                }
                else
                {
                    return "item:" + i;
                }
            }));

            i.UpdateImplementedAvatars(i.ImplementedAvatars.Select(i =>
            {
                if (i.StartsWith("<sys:temp>"))
                {
                    return i.Replace("<sys:temp>", "tempavatar:");
                }
                else if (i.StartsWith("<sys:commonavatar>"))
                {
                    return i.Replace("<sys:commonavatar>", "commonavatar:");
                }
                else
                {
                    return "item:" + i;
                }
            }));

#pragma warning disable CS0618 // 型またはメンバーが旧型式です
            i.UpdateCategory(new Models.Items.ItemCategory(i.Type, i.CustomCategory));
#pragma warning restore CS0618 // 型またはメンバーが旧型式です
        });

        var commonAvatars = CommonAvatars.GetAll();
        commonAvatars.ForEach(i =>
        {
            i.UpdateAvatars(i.Avatars.Select(i =>
            {
                if (i.StartsWith("<sys:temp>"))
                {
                    return i.Replace("<sys:temp>", "tempavatar:");
                }
                else if (i.StartsWith("<sys:commonavatar>"))
                {
                    return i.Replace("<sys:commonavatar>", "commonavatar:");
                }
                else
                {
                    return "item:" + i;
                }
            }));
        });

        var presets = BulkImportPresets.GetAll();
        presets.ForEach(i =>
        {
            i.Items.ForEach(i =>
            {
                i.UpdateItemId("item:" + i.ItemId);
            });
        });
    }
}
