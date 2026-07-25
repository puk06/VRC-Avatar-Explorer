using System.Linq;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI.Services;

public static class AppInitializer
{
    public static void Initialize()
    {
        AvatarExplorerApp.Instance.Initialize();

        Localizer.Instance.LoadFromFolder("locales");
        Localizer.Instance.SetLanguage(0);

        ContextMenuHandlerService.Initialize();
        RegisterBackupFiles();
        UserPreferencesService.Instance.Repository.Load();

        StartThumbnailCacheWampup();

        SingleInstanceService.StartServer();
    }

    private static void RegisterBackupFiles()
    {
        AvatarExplorerApp.Instance.BackupManager.AddTargetFile(SystemPath.UserPreferencesFilePath);
    }

    private static void StartThumbnailCacheWampup()
    {
        var thumbnailFileNames = AvatarExplorerApp.Instance.Items.GetAll()
            .Select(i => i.ThumbnailFileName)
            .Where(p => !string.IsNullOrEmpty(p));
        ImageService.StartThumbnailCacheWarmupInBackground(thumbnailFileNames);
    }
}
