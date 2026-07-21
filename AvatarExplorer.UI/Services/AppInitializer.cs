using System.Linq;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.ContextMenu;
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

        StartThumbnailCacheWampup();
    }

    private static void StartThumbnailCacheWampup()
    {
        var thumbnailFileNames = AvatarExplorerApp.Instance.Items.GetAll()
            .Select(i => i.ThumbnailFileName)
            .Where(p => !string.IsNullOrEmpty(p));
        ImageService.StartThumbnailCacheWarmupInBackground(thumbnailFileNames);
    }
}
