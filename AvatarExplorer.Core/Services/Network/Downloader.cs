using AvatarExplorer.Core.Services.IO;
using ErrorOr;

namespace AvatarExplorer.Core.Services.Network;

/// <summary>
/// HTTP 経由でファイルをダウンロードするユーティリティを提供します。
/// </summary>
public static class Downloader
{
    private const int BufferSize = 81920;

    /// <summary>
    /// 指定した URL からファイルをダウンロードし、指定パスに保存します。進捗は 0〜100 のパーセントで報告されます。
    /// </summary>
    /// <param name="url">ダウンロード元の URL。</param>
    /// <param name="filePath">保存先のファイルパス。</param>
    /// <param name="overwrite">既存ファイルを上書きするかどうか。false の場合、既存ファイルがあればそのまま成功とみなします。</param>
    /// <param name="reportProgress">進捗（パーセント）を報告するコールバック。省略可。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>ダウンロードに成功した、または既存ファイルを再利用した場合は Success。失敗またはキャンセル時はエラー。</returns>
    public static async Task<ErrorOr<Success>> Fetch(string url, string filePath, bool overwrite = false, Func<int, Task>? reportProgress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath)) return Error.Failure(description: "Download destination path is empty.");
        if (!overwrite && File.Exists(filePath)) return Result.Success;

        try
        {
            FileSystemService.PrepareFileDirectory(filePath);

            using var response = await HttpService.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

            var buffer = new byte[BufferSize];
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long totalRead = 0;
            var lastPercent = -1;

            await ReportProgress(reportProgress, 0, lastPercent);
            lastPercent = 0;

            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;

                var percent = CalculatePercent(totalRead, totalBytes);
                await ReportProgress(reportProgress, percent, lastPercent);
                lastPercent = percent;
            }

            await ReportProgress(reportProgress, 100, lastPercent);

            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            return Error.Failure(description: "Download was canceled.");
        }
        catch (Exception ex)
        {
            return Error.Failure(description: $"Download failed: {ex}");
        }
    }

    private static int CalculatePercent(long current, long total)
    {
        var percent = total > 0 ? (int)Math.Round((double)current / total * 100) : -1;
        return percent;
    }

    private static async Task ReportProgress(Func<int, Task>? reportProgress, int percent, int lastPercent)
    {
        if (reportProgress == null) return;
        if (percent == lastPercent || percent is < 0 or > 100) return;

        await reportProgress.Invoke(percent);
    }
}
