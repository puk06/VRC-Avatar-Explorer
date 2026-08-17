using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Services;

public static class InstanceRepository
{
    // Core
    public static AvatarExplorerApp App => AvatarExplorerApp.Instance;
    public static ItemRepository Items => App.ItemRepository;
    public static CommonAvatarRepository CommonAvatars => App.CommonAvatarRepository;
    public static TempAvatarRepository TempAvatars => App.TempAvatarRepository;
    public static BulkImportPresetRepository BulkImportPresets => App.BulkImportPresetRepository;
    public static VariationHashRepository VariationHashes => App.VariationHashRepository;
    public static ItemGroupService ItemGroupService => App.ItemGroupService;

    public static ItemNavigationService NavigationService => App.ItemNavigationService;
    public static RuntimeSettingsRepository RuntimeSettingsRepository => App.RuntimeSettingsRepository;
    public static RuntimeSettings RuntimeSettings => App.RuntimeSettings;
    public static BackupManager BackupManager => App.BackupManager;

    // UI
    public static MainWindowViewModel MainWindow => MainWindowViewModel.Instance;
    public static MainViewModel MainView => MainWindow.MainVM;
    public static UserPreferencesRepository UserPreferencesRepository => UserPreferencesService.Instance.Repository;
    public static UserPreferences UserPreferences => UserPreferencesRepository.Settings;
}
