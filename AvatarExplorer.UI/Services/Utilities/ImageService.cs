using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Models.ContextMenu;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class ImageService
{
    internal static readonly Dictionary<string, Bitmap?> SystemIconsDictionary = new()
    {
        { SystemIconKey.FolderIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/FolderIcon.png")) },
        { SystemIconKey.FileIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/FileIcon.png")) },
        { SystemIconKey.GroupIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/GroupIcon.png")) },
        { SystemIconKey.AvatarIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/AvatarIcon.png")) }
    };

    internal static bool IsSystemIcon(string fileName) => SystemIconsDictionary.ContainsKey(fileName);

    internal static Bitmap? Get(string fileName, IconType iconType = IconType.None)
    {
        if (IsSystemIcon(fileName)) return SystemIconsDictionary[fileName];

        return iconType switch
        {
            IconType.Item => Load(Path.Join(SystemPath.ItemThumbnailsPath, fileName)),
            _ => Load(fileName),
        };
    }

    internal static Bitmap? Load(string filePath) => File.Exists(filePath) ? new Bitmap(filePath) : null;
    internal static Bitmap? Load(Uri uri)
    {
        if (!AssetLoader.Exists(uri)) return null;

        using Stream fileStream = AssetLoader.Open(uri);
        return new Bitmap(fileStream);
    }
}
