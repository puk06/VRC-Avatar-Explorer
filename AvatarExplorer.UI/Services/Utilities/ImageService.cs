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
using AvatarExplorer.UI.Models.ContextMenu;

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

    private static readonly Dictionary<string, CacheEntry> BitmapCache = new();
    private static readonly Lock BitmapCacheLock = new();
    private static int ThumbnailWarmupStarted = 0;
    private static int _compressedThumbnailMaxEdge = DefaultCompressedThumbnailMaxEdge;

    internal static event Action<bool>? ThumbnailCacheWarmupStateChanged;

    internal static readonly Dictionary<string, Bitmap?> SystemIconsDictionary = new()
    {
        { SystemIconKey.None, null },
        { SystemIconKey.FolderIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/FolderIcon.png")) },
        { SystemIconKey.FileIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/FileIcon.png")) },
        { SystemIconKey.GroupIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/GroupIcon.png")) },
        { SystemIconKey.AvatarIcon, Load(new Uri("avares://AvatarExplorer/Assets/Internal/AvatarIcon.png")) }
    };

    internal static bool IsSystemIcon(string fileName) => SystemIconsDictionary.ContainsKey(fileName);

    internal static bool SetThumbnailCompressionMaxEdge(int maxEdge)
    {
        int clampedMaxEdge = Math.Clamp(maxEdge, 64, 2048);

        lock (BitmapCacheLock)
        {
            if (_compressedThumbnailMaxEdge == clampedMaxEdge) return true;

            // キャッシュが生成された後は圧縮サイズを固定して、既存キャッシュの破棄を避ける
            if (BitmapCache.Count > 0) return false;

            _compressedThumbnailMaxEdge = clampedMaxEdge;
            return true;
        }
    }

    internal static Bitmap? Get(string fileName, IconType iconType = IconType.None)
    {
        try
        {
            if (IsSystemIcon(fileName)) return SystemIconsDictionary[fileName];

            string filePath = iconType switch
            {
                IconType.Item => Path.Join(SystemPath.ItemThumbnailsFolderPath, fileName),
                _ => fileName,
            };

            bool compressThumbnail = iconType == IconType.Item;
            return GetFromFileCache(filePath, compressThumbnail);
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
            using Stream fileStream = AssetLoader.Open(uri);
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

                foreach (string filePath in imageFileNames)
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
        bool exists = File.Exists(filePath);
        DateTime lastWriteTimeUtc = DateTime.MinValue;
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
            if (BitmapCache.TryGetValue(filePath, out CacheEntry? cacheEntry) && cacheEntry.Exists == exists && cacheEntry.LastWriteTimeUtc == lastWriteTimeUtc)
            {
                return cacheEntry.Bitmap;
            }

            Bitmap? bitmap = exists ? LoadBitmap(filePath, compressThumbnail) : null;

            if (cacheEntry?.Bitmap != null && !ReferenceEquals(cacheEntry.Bitmap, bitmap))
            {
                cacheEntry.Bitmap.Dispose();
            }

            BitmapCache[filePath] = new CacheEntry
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

            PixelSize sourceSize = sourceBitmap.PixelSize;
            int maxEdge = Math.Max(sourceSize.Width, sourceSize.Height);
            if (maxEdge <= _compressedThumbnailMaxEdge) return sourceBitmap;

            double scale = (double)_compressedThumbnailMaxEdge / maxEdge;
            PixelSize targetSize = new(
                Math.Max(1, (int)Math.Round(sourceSize.Width * scale)),
                Math.Max(1, (int)Math.Round(sourceSize.Height * scale))
            );

            Bitmap compressedBitmap = sourceBitmap.CreateScaledBitmap(targetSize, BitmapInterpolationMode.HighQuality);
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
