using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Data.Paths;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI.Services;

public static class AppInitializer
{
    public static void InitializeApp()
    {
        InstanceRepository.App.Initialize();
    }

    public static void InitializeLocalization(string localizationFolderPath)
    {
        Localizer.Instance.LoadFromFolder(localizationFolderPath);
    }

    public static void InitializeContextMenu()
    {
        ContextMenuHandlerService.Initialize();
    }

    public static void InitializeUserPreferences()
    {
        InstanceRepository.UserPreferencesRepository.Load();
    }

    public static void StartThumbnailCacheWarmup()
    {
        var thumbnailFileNames = InstanceRepository.Items.GetAll()
            .Select(i => i.ThumbnailFileName)
            .Where(p => !string.IsNullOrEmpty(p));
        ImageService.StartThumbnailCacheWarmupInBackground(thumbnailFileNames);
    }

    public static void StartSingleInstanceService()
    {
        SingleInstanceService.StartServer();
    }

    public static void RegisterBackupFiles()
    {
        InstanceRepository.BackupManager.AddTargetFiles(
            [
                UISystemPath.UserPreferencesFilePath
            ]
        );
    }
}
