using System.Globalization;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

internal class BackupManager
{
    private int _backupInterval = TimeUtils.MinToMs(5);
    private CancellationTokenSource? _backupCts;
    private Task? _backupTask;
    private DateTime _lastBackupDate = DateTime.MinValue;
    private string _backupRootFolderPath = string.Empty;

    internal void StartAutoBackup(int interval, string backupRootFolderPath)
    {
        SetAutoBackupPath(backupRootFolderPath);
        SetAutoBackupInterval(TimeUtils.MinToMs(interval));

        if (_backupTask != null) return;

        _backupCts = new CancellationTokenSource();
        _backupTask = Task.Run(() => AutoBackupLoop(_backupCts.Token));
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

    internal void SetAutoBackupInterval(int interval)
    {
        if (interval < 0) return;
        _backupInterval = TimeUtils.MinToMs(interval);
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
            await ExecuteBackup(_backupRootFolderPath, token);
            await Task.Delay(_backupInterval, token);
        }
    }
    private static readonly string[] _backupFiles =
    [
        SystemPath.ItemDatabasePath,
        SystemPath.CommonAvatarDatabasePath,
        SystemPath.RuntimeSettingsFilePath,
        SystemPath.UserPreferencesFilePath,
        SystemPath.BulkImportPresetDatabasePath
    ];

    internal async Task<ErrorOr<Success>> ExecuteBackup(string backupRootFolderPath, CancellationToken token = default)
    {
        try
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);
            string backupFolderPath = Path.Combine(backupRootFolderPath, now);
            
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
            var filesToBackup = _backupFiles.Where(File.Exists).ToList();

            foreach (string filePath in filesToBackup)
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

                string fileName = Path.GetFileName(filePath);
                string backupPath = Path.Combine(backupFolderPath, fileName);

                ErrorOr<Success> result = await FileSystemService.CopyFileAsync(filePath, backupPath);
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

            string errorMessage = $"Backup completed with errors: {successCount} succeeded, {failureCount} failed.";
            ErrorManager.Instance.PostInternalError(errorMessage);
            return Error.Failure(description: errorMessage);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to execute backup.", ex);
            return Error.Failure(description: "Failed to execute backup.");
        }
    }
}
