using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Network;

public static class Downloader
{
    private const int BufferSize = 81920;

    public static async Task<bool> Fetch(string url, string filePath, bool overwrite = false, Func<int, Task>? reportProgress = null, CancellationToken ct = default)
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
            int lastPercent = -1;

            if (reportProgress != null) await reportProgress.Invoke(0);

            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)Math.Round((double)totalRead / totalBytes * 100);
                    if (percent != lastPercent && percent is >= 0 and <= 100)
                    {
                        lastPercent = percent;
                        if (reportProgress != null) await reportProgress.Invoke(percent);
                    }
                }
            }

            if (reportProgress != null) await reportProgress.Invoke(100);

            return true;
        }
        catch (OperationCanceledException)
        {
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
