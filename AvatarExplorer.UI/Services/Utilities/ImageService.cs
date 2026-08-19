using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Data;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class ImageService
{
    private const int DefaultCompressedThumbnailMaxEdge = 256;

    private sealed class CacheEntry
    {
        internal Bitmap? Bitmap { get; init; }
        internal DateTime LastWriteTimeUtc { get; init; }
        internal bool Exists { get; init; }
    }

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff", ".tif"
    };

    private static readonly Dictionary<string, CacheEntry> BitmapCache = new();
    private static readonly Lock BitmapCacheLock = new();
    private static int ThumbnailWarmupStarted = 0;
    private static int _compressedThumbnailMaxEdge = DefaultCompressedThumbnailMaxEdge;

    internal static event Action<bool>? ThumbnailCacheWarmupStateChanged;

    private const string ResourceRootPath = "avares://AvatarExplorer/Assets/Internal/";
    private static Uri GetAssetUri(string fileName) => new(ResourceRootPath + fileName);
    internal static readonly Dictionary<string, Bitmap?> SystemIconsDictionary = new()
    {
        { SystemIconKey.None, null },
        { SystemIconKey.FolderIcon, Load(GetAssetUri("FolderIcon.png")) },
        { SystemIconKey.HiddenFolderIcon, Load(GetAssetUri("HiddenFolderIcon.png")) },
        { SystemIconKey.FileIcon, Load(GetAssetUri("FileIcon.png")) },
        { SystemIconKey.UnknownFileIcon, Load(GetAssetUri("UnknownFileIcon.png")) },
        { SystemIconKey.GroupIcon, Load(GetAssetUri("GroupIcon.png")) },
        { SystemIconKey.AvatarIcon, Load(GetAssetUri("AvatarIcon.png")) },
        { SystemIconKey.LinkIcon, Load(GetAssetUri("LinkIcon.png")) },
    };

    internal static bool IsSystemIcon(string fileName) => SystemIconsDictionary.ContainsKey(fileName);

    internal static bool IsImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(ext) && SupportedImageExtensions.Contains(ext);
    }

    internal static Bitmap? GetFromFileSystem(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            return LoadBitmap(filePath, compressThumbnail: true);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to get image from file system: {filePath}", ex);
            return null;
        }
    }

    internal static Bitmap? Get(string fileName)
    {
        try
        {
            if (IsSystemIcon(fileName)) return SystemIconsDictionary[fileName];

            var filePath = Path.Join(SystemPath.ItemThumbnailsFolderPath, fileName);
            return GetFromFileCache(filePath, compressThumbnail: true);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to get image for file: {fileName}", ex);
            return null;
        }
    }

    internal static Bitmap? Load(Uri uri)
    {
        if (!AssetLoader.Exists(uri)) return null;

        try
        {
            using var fileStream = AssetLoader.Open(uri);
            return new Bitmap(fileStream);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load bitmap from URI: {uri}", ex);
            return null;
        }
    }
    
    internal static void StartThumbnailCacheWarmupInBackground(IEnumerable<string> imageFileNames)
    {
        if (Interlocked.Exchange(ref ThumbnailWarmupStarted, 1) != 0) return;

        ThumbnailCacheWarmupStateChanged?.Invoke(true);

        _ = Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(SystemPath.ItemThumbnailsFolderPath)) return;

                foreach (var filePath in imageFileNames)
                {
                    _ = GetFromFileCache(Path.Join(SystemPath.ItemThumbnailsFolderPath, filePath), compressThumbnail: true);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError("Failed to warmup thumbnail cache in background.", ex);
            }
            finally
            {
                ThumbnailCacheWarmupStateChanged?.Invoke(false);
            }
        });
    }

    private static Bitmap? GetFromFileCache(string filePath, bool compressThumbnail)
    {
        var exists = File.Exists(filePath);
        var lastWriteTimeUtc = DateTime.MinValue;
        if (exists)
        {
            try
            {
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"Failed to get last write time for file: {filePath}", ex);
                return null;
            }
        }

        lock (BitmapCacheLock)
        {
            if (BitmapCache.TryGetValue(filePath, out var cacheEntry) && cacheEntry.Exists == exists && cacheEntry.LastWriteTimeUtc == lastWriteTimeUtc)
            {
                return cacheEntry.Bitmap;
            }

            var bitmap = exists ? LoadBitmap(filePath, compressThumbnail) : null;

            if (cacheEntry?.Bitmap != null && !ReferenceEquals(cacheEntry.Bitmap, bitmap))
            {
                cacheEntry.Bitmap.Dispose();
            }

            BitmapCache[filePath] = new()
            {
                Bitmap = bitmap,
                LastWriteTimeUtc = lastWriteTimeUtc,
                Exists = exists,
            };

            return bitmap;
        }
    }

    private static Bitmap? LoadBitmap(string filePath, bool compressThumbnail)
    {
        Bitmap? sourceBitmap = null;

        try
        {
            sourceBitmap = new(filePath);
            if (!compressThumbnail) return sourceBitmap;

            var sourceSize = sourceBitmap.PixelSize;
            int maxEdge = Math.Max(sourceSize.Width, sourceSize.Height);
            if (maxEdge <= _compressedThumbnailMaxEdge) return sourceBitmap;

            var scale = (double)_compressedThumbnailMaxEdge / maxEdge;
            var targetSize = new PixelSize(
                Math.Max(1, (int)Math.Round(sourceSize.Width * scale)),
                Math.Max(1, (int)Math.Round(sourceSize.Height * scale))
            );

            var compressedBitmap = sourceBitmap.CreateScaledBitmap(targetSize, BitmapInterpolationMode.HighQuality);
            sourceBitmap.Dispose();

            return compressedBitmap;
        }
        catch (Exception ex)
        {
            sourceBitmap?.Dispose();
            ErrorManager.Instance.PostInternalError($"Failed to load or compress bitmap: {filePath}", ex);
            return null;
        }
    }
}
