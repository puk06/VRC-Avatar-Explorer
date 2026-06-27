using System.Collections.Immutable;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Database;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    public static readonly string CurrentVersion = "2.7.0-beta.4";

    private static readonly AvatarExplorerApp _instance = new();
    public static AvatarExplorerApp Instance => _instance;

    private bool _initialized = false;

    private readonly DatabaseManager<Item> _itemDatabaseManager = new(SystemPath.ItemDatabasePath);
    private readonly DatabaseManager<CommonAvatar> _commonAvatarDatabaseManager = new(SystemPath.CommonAvatarDatabasePath);
    private readonly DatabaseManager<TempAvatar> _tempAvatarsDatabaseManager = new(SystemPath.TempAvatarsDatabasePath);
    private readonly DatabaseManager<BulkImportPreset> _bulkImportPresetDatabaseManager = new(SystemPath.BulkImportPresetDatabasePath);

    private readonly Dictionary<string, string> _itemSearchIndexDictionary = new();

    public Func<ArchivePasswordRequest, ValueTask<string?>>? PasswordProvider { get; set; }

    private readonly SelectionState _selectionState = new();
    private readonly Dictionary<ItemTagStates, Func<SelectionNode, ImmutableArray<ItemCountInfo>>> _stateHandlers;

    private readonly SettingsManager<RuntimeSettings> _runtimeSettingsManager = new(SystemPath.RuntimeSettingsFilePath);
    private RuntimeSettings RuntimeSettings => _runtimeSettingsManager.Settings;

    private readonly BackupManager _backupManager = new();

    private AvatarExplorerApp()
    {
        _stateHandlers = new()
        {
            { ItemTagStates.SearchItem, HandleRootSelectedItem },
            { ItemTagStates.RootAvatar, HandleRootAvatar },
            { ItemTagStates.RootAuthor, HandleRootAuthor },
            { ItemTagStates.RootCategory, HandleRootCategory },
            { ItemTagStates.RootItem, HandleRootSelectedItem },
            { ItemTagStates.RootSelectedCategory, HandleRootSelectedCategory },
            { ItemTagStates.RootSelectedItem, HandleRootSelectedItem },
            { ItemTagStates.ItemFolder, HandleItemFolder },
            { ItemTagStates.ItemFileCategory, HandleItemFileCategory }
        };
    }

    public void Initialize()
    {
        if (_initialized) return;

        LoadItemDatabase();
        LoadCommonAvatarDatabase();
        LoadBulkImportPresetDatabase();
        LoadTempAvatarsDatabase();
        LoadRuntimeSettings();
        StartAutoBackup();

        UpdateSearchIndex();
        EnsureAllItemsDefaultPathExist();

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;

        _initialized = true;
    }
}
