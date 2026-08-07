using System.Linq;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Data.Paths;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI.Services;

public static class AppInitializer
{
    public static void InitializeApp()
    {
        AvatarExplorerApp.Instance.Initialize();
    }

    public static void InitializeLocalization()
    {
        Localizer.Instance.LoadFromFolder("locales");
    }

    public static void InitializeContextMenu()
    {
        ContextMenuHandlerService.Initialize();
    }

    public static void InitializeUserPreferences()
    {
        UserPreferencesService.Instance.Repository.Load();
    }

    public static void StartThumbnailCacheWarmup()
    {
        var thumbnailFileNames = AvatarExplorerApp.Instance.Items.GetAll()
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
        AvatarExplorerApp.Instance.BackupManager.AddTargetFiles(
            [
                UISystemPath.UserPreferencesFilePath,
                UISystemPath.UserPreferencesMigrationVersionPath
            ]
        );
    }
}
