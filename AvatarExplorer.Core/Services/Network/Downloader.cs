using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Network;

internal static class Downloader
{
    private const int BufferSize = 81920;

    internal static async Task<bool> Fetch(string url, string filePath, bool overwrite = false, IProgress<(long downloaded, long? total)>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath)) return false;

        if (!overwrite && File.Exists(filePath)) return true;

        try
        {
            FileSystemService.PrepareFileDirectory(filePath);

            using var response = await HttpService.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

            var buffer = new byte[BufferSize];
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long totalRead = 0;

            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;
                progress?.Report((totalRead, totalBytes >= 0 ? totalBytes : (long?)null));
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            // Download was canceled, return false to indicate failure
            return false;
        }
        catch (Exception ex)
        {
            var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "unknown";
            ErrorManager.Instance.PostInternalError($"Failed to download file from '{host}'.", ex);
            return false;
        }
    }
}
