using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Network;

internal static class ImageDownloader
{
    internal static async Task<bool> Fetch(string url, string filePath, bool overwrite = false)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        if (!overwrite && File.Exists(filePath)) return true;

        try
        {
            var imageBytes = await GetBytes(url);
            FileSystemService.PrepareFileDirectory(filePath);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            return true;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to download image: '{url}'.", ex);
            return false;
        }
    }
    private static async Task<byte[]> GetBytes(string url) => await HttpService.Client.GetByteArrayAsync(url);
}
