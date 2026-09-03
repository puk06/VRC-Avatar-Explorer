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

    // UIスレッドからは Peek のみを呼び出すこと (Peek はファイルI/Oを行わない)。
    // 読み込みは必ず GetAsync / GetFromFileSystem をバックグラウンドで行い、結果を BitmapCache に反映する。
    private static readonly Dictionary<string, CacheEntry> BitmapCache = [];
    private static readonly Dictionary<string, Task<Bitmap?>> InFlightLoads = [];
    private static readonly Lock BitmapCacheLock = new();
    private static int ThumbnailWarmupStarted = 0;
    private static int _compressedThumbnailMaxEdge = DefaultCompressedThumbnailMaxEdge;

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

    /// <summary>
    /// キャッシュ (またはシステムアイコン) から即時に取得する。ファイルI/Oを行わないためUIスレッドから呼び出せる。
    /// キャッシュに無い場合は null を返すので、呼び出し側はフォールバック表示 + GetAsync での後読みを行う。
    /// </summary>
    internal static Bitmap? Peek(string fileName)
    {
        if (IsSystemIcon(fileName)) return SystemIconsDictionary.GetValueOrDefault(fileName);

        var filePath = Path.Join(SystemPath.ItemThumbnailsFolderPath, fileName);
        lock (BitmapCacheLock)
        {
            return BitmapCache.TryGetValue(filePath, out var entry) ? entry.Bitmap : null;
        }
    }

    /// <summary>
    /// サムネイルをバックグラウンドで取得する。読み込みはファイル単位で重複排除され、結果はキャッシュされる。
    /// 返される Bitmap はキャッシュ所有 (共有) のため、呼び出し側で Dispose してはいけない。
    /// </summary>
    internal static Task<Bitmap?> GetAsync(string fileName)
    {
        try
        {
            if (IsSystemIcon(fileName)) return Task.FromResult(SystemIconsDictionary.GetValueOrDefault(fileName));

            var filePath = Path.Join(SystemPath.ItemThumbnailsFolderPath, fileName);
            lock (BitmapCacheLock)
            {
                if (InFlightLoads.TryGetValue(filePath, out var inFlight)) return inFlight;

                var loadTask = Task.Run(() => LoadItemThumbnailAsync(filePath));
                InFlightLoads[filePath] = loadTask;
                return loadTask;
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to get image for file: {fileName}", ex);
            return Task.FromResult<Bitmap?>(null);
        }
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

    /// <summary>
    /// 全サムネイルのキャッシュをバックグラウンドで順次構築する。
    /// GetAsync と同じ経路を通るため、表示中アイテムの読み込みと重複することはない。
    /// </summary>
    internal static void StartThumbnailCacheWarmupInBackground(IEnumerable<string> imageFileNames)
    {
        if (Interlocked.Exchange(ref ThumbnailWarmupStarted, 1) != 0) return;

        _ = Task.Run(async () =>
        {
            try
            {
                if (!Directory.Exists(SystemPath.ItemThumbnailsFolderPath)) return;

                foreach (var fileName in imageFileNames.Where(n => !string.IsNullOrEmpty(n) && !IsSystemIcon(n)))
                {
                    await GetAsync(fileName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError("Failed to warmup thumbnail cache in background.", ex);
            }
        });
    }

    private static async Task<Bitmap?> LoadItemThumbnailAsync(string filePath)
    {
        try
        {
            var (exists, lastWriteTimeUtc) = GetFileState(filePath);

            lock (BitmapCacheLock)
            {
                if (BitmapCache.TryGetValue(filePath, out var entry) && entry.Exists == exists && entry.LastWriteTimeUtc == lastWriteTimeUtc)
                {
                    return entry.Bitmap;
                }
            }

            var bitmap = exists ? LoadBitmap(filePath, compressThumbnail: true) : null;

            lock (BitmapCacheLock)
            {
                // 差し替え前の Bitmap は描画中のViewModelから参照されている可能性があるため Dispose しない (GCに回収を委ねる)
                BitmapCache[filePath] = new()
                {
                    Bitmap = bitmap,
                    LastWriteTimeUtc = lastWriteTimeUtc,
                    Exists = exists,
                };
            }

            return bitmap;
        }
        finally
        {
            lock (BitmapCacheLock)
            {
                InFlightLoads.Remove(filePath);
            }
        }
    }

    private static (bool Exists, DateTime LastWriteTimeUtc) GetFileState(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return (false, DateTime.MinValue);
            return (true, File.GetLastWriteTimeUtc(filePath));
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to get last write time for file: {filePath}", ex);
            return (false, DateTime.MinValue);
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

    public static void UpdateCompressedThumbnailMaxEdge(int maxEdge)
    {
        _compressedThumbnailMaxEdge = maxEdge;
    }
}
