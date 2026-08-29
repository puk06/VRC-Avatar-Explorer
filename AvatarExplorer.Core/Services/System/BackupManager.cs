using System.Globalization;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

/// <summary>データベースファイルなどのバックアップ対象を管理し、自動バックアップと復元を行うマネージャー。</summary>
public class BackupManager
{
    /// <summary>バックアップから復元が完了したときに発生するイベント。</summary>
    public event Action? OnBackupRestored;

    private readonly HashSet<string> BackupFiles = [];
    /// <summary>バックアップ対象のファイルを1件追加します。</summary>
    /// <param name="path">バックアップ対象とするファイルのパス。</param>
    public void AddTargetFile(string path) => BackupFiles.Add(path);
    /// <summary>バックアップ対象のファイルを複数追加します。</summary>
    /// <param name="paths">バックアップ対象とするファイルのパス一覧。</param>
    public void AddTargetFiles(string[] paths) => BackupFiles.UnionWith(paths);

    private int _backupInterval = TimeUtils.MinToMs(5);
    private CancellationTokenSource? _backupCts;
    private Task? _backupTask;
    private DateTime _lastBackupDate = DateTime.MinValue;
    private string _backupRootFolderPath = string.Empty;

    internal void StartAutoBackup(int intervalMinutes, string backupRootFolderPath)
    {
        SetAutoBackupPath(backupRootFolderPath);
        SetAutoBackupInterval(intervalMinutes);

        if (_backupTask != null) return;

        _backupCts = new CancellationTokenSource();
        _backupTask = Task.Run(() => AutoBackupLoop(_backupCts.Token), cancellationToken: CancellationToken.None);
    }

    internal async Task StopAutoBackup()
    {
        if (_backupCts != null)
        {
            await _backupCts.CancelAsync();
            if (_backupTask != null) await _backupTask;

            _backupCts.Dispose();
            _backupCts = null;
            _backupTask = null;
        }
    }

    internal DateTime LastBackupTime => _lastBackupDate;

    internal void SetAutoBackupInterval(int intervalMinutes)
    {
        if (intervalMinutes < 0) return;
        _backupInterval = TimeUtils.MinToMs(intervalMinutes);
    }

    internal void SetAutoBackupPath(string path)
    {
        _backupRootFolderPath = path;
    }

    private async Task AutoBackupLoop(CancellationToken token)
    {
        await Task.Delay(TimeUtils.MinToMs(1), token); // 1分は待機する

        while (!token.IsCancellationRequested)
        {
            await ExecuteBackup(token);
            await Task.Delay(_backupInterval, token);
        }
    }

    /// <summary>
    /// 登録されたバックアップ対象ファイルを、タイムスタンプ付きのフォルダにコピーしてバックアップを作成します。
    /// キャンセルされた場合や一部でも失敗した場合は不完全なフォルダを削除し、エラーを返します。
    /// </summary>
    /// <param name="token">バックアップのキャンセルに使用するキャンセルトークン。</param>
    /// <returns>成功した場合は <see cref="Success"/>、失敗した場合はエラー情報。</returns>
    public async Task<ErrorOr<Success>> ExecuteBackup(CancellationToken token = default)
    {
        try
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);
            var backupFolderPath = Path.Combine(_backupRootFolderPath, now);

            try
            {
                Directory.CreateDirectory(backupFolderPath);
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"Failed to create backup folder: {backupFolderPath}.", ex);
                return Error.Failure(description: "Failed to create backup folder.");
            }

            int successCount = 0;
            int failureCount = 0;
            var filesToBackup = BackupFiles.Where(File.Exists);

            foreach (var filePath in filesToBackup)
            {
                if (token.IsCancellationRequested)
                {
                    // Clean up incomplete backup folder on cancellation
                    try
                    {
                        Directory.Delete(backupFolderPath, recursive: true);
                    }
                    catch { /* Ignore cleanup errors */ }
                    return Result.Success;
                }

                var fileName = Path.GetFileName(filePath);
                var backupPath = Path.Combine(backupFolderPath, fileName);

                var result = await FileSystemService.CopyFileAsync(filePath, backupPath);
                if (result.IsError)
                {
                    ErrorManager.Instance.PostInternalError($"Failed to copy file: {filePath}.", tag: result.Errors.ToErrorString());
                    failureCount++;
                }
                else
                {
                    successCount++;
                }
            }

            // Only mark backup as successful if all files were copied successfully
            if (failureCount == 0 && successCount > 0)
            {
                _lastBackupDate = DateTime.Now;
                return Result.Success;
            }

            // If some or all files failed to copy, clean up and return error
            try
            {
                Directory.Delete(backupFolderPath, recursive: true);
            }
            catch { /* Ignore cleanup errors */ }

            var errorMessage = $"Backup completed with errors: {successCount} succeeded, {failureCount} failed.";
            ErrorManager.Instance.PostInternalError(errorMessage);
            return Error.Failure(description: errorMessage);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to execute backup.", ex);
            return Error.Failure(description: "Failed to execute backup.");
        }
    }

    /// <summary>
    /// 指定したフォルダからバックアップを復元します。復元前に現在の状態を自動バックアップし、
    /// 対象ファイルを元の保存先に上書きコピーした後、<see cref="OnBackupRestored"/> イベントを発火します。
    /// </summary>
    /// <param name="folderPath">復元元となるバックアップフォルダのパス。</param>
    public async Task RestoreBackup(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        await ExecuteBackup(CancellationToken.None); // Backup current state before restoring

        foreach (var file in FileSystemService.EnumerateFiles(folderPath))
        {
            var fileName = Path.GetFileName(file);
            var sourcePath = BackupFiles.FirstOrDefault(i => Path.GetFileName(i) == fileName);

            // Handle special case for migration version files
            if (sourcePath == null && fileName.EndsWith(".migration.version"))
            {
                var baseFileName = fileName[..^".migration.version".Length];
                var baseSourcePath = BackupFiles.FirstOrDefault(i => Path.GetFileName(i) == baseFileName);
                if (baseSourcePath != null)
                {
                    sourcePath = baseSourcePath + ".migration.version";
                }
            }

            if (sourcePath == null) continue;

            await FileSystemService.CopyFileAsync(file, sourcePath);
        }

        OnBackupRestored?.Invoke();
    }
}
