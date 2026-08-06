using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;
using ErrorOr;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace AvatarExplorer.Core.Services.IO;

public record CopyResult
{
    public int SuccessCount { get; init; }
    public int TotalCount { get; init; }
    public List<CopyFailure> Failures { get; init; } = new();
}
public record CopyFailure
{
    public string SourcePath { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

public record ExtractResult
{
    public string ItemParentFolder { get; set; } = string.Empty;
    public List<string> FolderPaths { get; } = new();
    public List<string> ProcessingFailedPaths { get; init; } = new();
}

public class ModifiedUnitypackagesResult
{
    public bool IsError { get; set; } = true;
    public string? ModifiedUnitypackagePath { get; set; } = null;
    public List<string> Success { get; } = new();
    public List<string> Failed { get; } = new();
}

public class ItemPathEntry
{
    public string FileName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsUrl { get; set; } = false;
}

public static class FileSystemService
{
    private const int BufferSize = 1024 * 1024;

    #region Serialize / Deserialize
    public static ErrorOr<Success> SerializeClass<T>(T value, string filePath) where T : class
    {
        try
        {
            PrepareFileDirectory(filePath);
            var json = JsonManager.Serialize(value);
            File.WriteAllText(filePath, json);
            return Result.Success;
        }
        catch (Exception ex)
        {
            var elementType = typeof(T).GetGenericArguments().FirstOrDefault() ?? typeof(T);
            ErrorManager.Instance.PostInternalError($"Failed to serialize class: '{elementType.Name}' to '{filePath}'.", ex);
            return Error.Failure(description: "Failed to serialize class.");
        }
    }
    public static ErrorOr<T> DeserializeClass<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath)) return Error.NotFound(description: $"File not found: {filePath}");
            
            var json = File.ReadAllText(filePath);
            var result = JsonManager.Deserialize<T>(json);
            
            if (result == null) return Error.Failure(description: "deserialization result is null.");
            
            return result;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to desearize class: '{typeof(T).Name}' from '{filePath}'.", ex);
            return Error.Failure(description: "Failed to desearize class.");
        }
    }
    #endregion

    #region Unitypackage Modifier
    public static async Task<ErrorOr<List<string>>> GetUnitypackagePathnamesAsync(string unitypackagePath)
    {
        if (string.IsNullOrWhiteSpace(unitypackagePath))
            return Error.Validation(description: "Unitypackage path is empty.");

        if (!File.Exists(unitypackagePath))
            return Error.NotFound(description: $"Unitypackage not found: '{unitypackagePath}'.");

        var pathnames = new List<string>();

        try
        {
            await using var fileStream = File.OpenRead(unitypackagePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            await using var tarReader = new TarReader (gzipStream);

            while (await tarReader.GetNextEntryAsync() is { } entry)
            {
                if (Path.GetFileName(entry.Name) != "pathname" || entry.DataStream == null)
                    continue;

                using var reader = new StreamReader (entry.DataStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var pathname = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(pathname))
                {
                    pathnames.Add(pathname.Trim());
                }
            }

            return pathnames;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to read pathname entries from unitypackage: '{unitypackagePath}'.", ex);
            return Error.Failure(description: "Failed to read pathname entries from unitypackage.");
        }
    }

    public static async Task<ErrorOr<string>> ExtractUnitypackageAssetAsync(string unitypackagePath, string pathname, string destinationFolderPath)
    {
        if (string.IsNullOrWhiteSpace(unitypackagePath))
            return Error.Validation(description: "Unitypackage path is empty.");

        if (!File.Exists(unitypackagePath))
            return Error.NotFound(description: $"Unitypackage not found: '{unitypackagePath}'.");

        if (string.IsNullOrWhiteSpace(pathname))
            return Error.Validation(description: "Unitypackage pathname is empty.");

        if (string.IsNullOrWhiteSpace(destinationFolderPath))
            return Error.Validation(description: "Destination folder path is empty.");

        var normalizedTargetPath = NormalizeUnitypackagePath(pathname);
        string? targetGroupFolder = null;

        try
        {
            await TarGzReader(unitypackagePath, async entry =>
            {
                if (Path.GetFileName(entry.Name) != "pathname" || entry.DataStream == null) return true;

                using var reader = new StreamReader(entry.DataStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                var entryPath = NormalizeUnitypackagePath(await reader.ReadToEndAsync());

                if (string.Equals(entryPath, normalizedTargetPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetGroupFolder = GetUnitypackageTopLevelFolder(entry.Name);
                    return false; // 終わらせる
                }
                
                return true;
            });
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to search pathname entries from unitypackage: '{unitypackagePath}'.", ex);
            return Error.Failure(description: "Failed to search pathname entries from unitypackage.");
        }

        if (string.IsNullOrWhiteSpace(targetGroupFolder))
            return Error.NotFound(description: $"Unitypackage entry not found: '{pathname}'.");

        var extractedFilePath = Path.Combine(destinationFolderPath, normalizedTargetPath.Replace('/', Path.DirectorySeparatorChar));
        var extractedDirectory = Path.GetDirectoryName(extractedFilePath);
        if (!string.IsNullOrWhiteSpace(extractedDirectory)) Directory.CreateDirectory(extractedDirectory);

        try
        {
            await using var fileStream = File.OpenRead(unitypackagePath);
            await using var gzipStream = new GZipStream (fileStream, CompressionMode.Decompress);
            await using var tarReader = new TarReader (gzipStream);

            while (await tarReader.GetNextEntryAsync() is { } entry)
            {
                if (entry.DataStream == null)
                    continue;

                if (!string.Equals(GetUnitypackageTopLevelFolder(entry.Name), targetGroupFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(Path.GetFileName(entry.Name), "asset", StringComparison.OrdinalIgnoreCase))
                    continue;

                await using var outputStream = File.Create(extractedFilePath);
                await entry.DataStream.CopyToAsync(outputStream);

                return extractedFilePath;
            }

            return Error.NotFound(description: $"Asset entry not found for pathname: '{pathname}'.");
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to export asset from unitypackage: '{unitypackagePath}'.", ex);
            return Error.Failure(description: "Failed to export asset from unitypackage.");
        }
    }

    private static string NormalizeUnitypackagePath(string path) => path.Trim().Replace('\\', '/');

    private static string GetUnitypackageTopLevelFolder(string entryName)
    {
        var normalizedEntryName = NormalizeUnitypackagePath(entryName);
        int separatorIndex = normalizedEntryName.IndexOf('/');
        return separatorIndex >= 0 ? normalizedEntryName[..separatorIndex] : normalizedEntryName;
    }

    public static async Task<ModifiedUnitypackagesResult> ModifyUnitypackageFilePathsAsync(IReadOnlyList<UnitypackageImportEntry> entries, Func<(string, int), Task>? reportProgress = null)
    {
        var result = new ModifiedUnitypackagesResult();

        try
        {
            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Unitypackage.Status.Preparing, 0));

            var saveFolderPath = PrepareSaveFolderPath();
            var unitypackagePath = saveFolderPath + ".unitypackage";

            if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Unitypackage.Status.Extracting, 10));

            var validEntries = new List<UnitypackageImportEntry>();
            int totalEntries = 0;
            foreach (var entry in entries)
            {
                try
                {
                    totalEntries += await CountTarEntriesAsync(entry.FilePath);
                    validEntries.Add(entry);
                }
                catch
                {
                    // 事前に処理に失敗する可能性があるものは削除しておく
                }
            }

            int currentProcessedEntries = 0;
            foreach (var entry in validEntries)
            {
                try
                {
                    currentProcessedEntries = await ExtractUnitypackageToFolderAsync(entry.FilePath, saveFolderPath, entry.CategoryDisplayName, totalEntries, currentProcessedEntries, reportProgress);
                    result.Success.Add(entry.FilePath);
                }
                catch
                {
                    result.Failed.Add(entry.FilePath);
                }
            }

            reportProgress?.Invoke((Loc.Processing.Unitypackage.Status.Creating, 90));

            await CreateTarArchive(saveFolderPath, unitypackagePath);

            DeleteDirectory(saveFolderPath, true);

            reportProgress?.Invoke((Loc.Processing.Unitypackage.Status.Completed, 100));

            await Task.Delay(500); // すぐ閉じるのではなく、100%の表記を出してから0.5秒経って返すようにする

            if (File.Exists(unitypackagePath))
            {
                result.IsError = false;
                result.ModifiedUnitypackagePath = unitypackagePath;

                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Error occured while processing unitypackage.", ex);
            result.IsError = true;

            return result;
        }
    }
    private static string PrepareSaveFolderPath()
    {
        var saveFolderPath = Path.Combine(GetNewTempFolder(), "Unitypackages Modified by Avatar Explorer");
        Directory.CreateDirectory(saveFolderPath);

        return saveFolderPath;
    }
    public static string GetNewTempFolder()
    {
        int i = 1;
        while (Directory.Exists(Path.Combine(SystemPath.TempFolderPath, i.ToString()))) i++;
        var resultPath = Path.Combine(SystemPath.TempFolderPath, i.ToString());
        Directory.CreateDirectory(resultPath);
        return resultPath;
    }
    private static async Task<int> CountTarEntriesAsync(string tarGzFilePath)
    {
        int count = 0;
        await TarGzReader(tarGzFilePath, _ => count++);
        return count;
    }
    private static async Task<int> ExtractUnitypackageToFolderAsync(string tarGzFilePath, string saveFilePath, string category, int totalEntries, int currentProcessedEntries = 0, Func<(string, int), Task>? reportProgress = null)
    {
        int processedEntries = currentProcessedEntries;

        int lastProgress = -1;

        await TarGzReader(tarGzFilePath, async entry =>
        {
            try
            {
                if (Path.GetFileName(entry.Name) == "pathname" && entry.DataStream != null)
                {
                    using StreamReader reader = new(entry.DataStream);
                    string assetPath = await reader.ReadToEndAsync();

                    // 親フォルダがAssetsのものだけ変更するようにする (例えば、親フォルダがPackagesのものは変更しない)
                    if (assetPath.StartsWith("Assets")) assetPath = assetPath.Insert(7, $"{category}/");

                    entry.DataStream = new MemoryStream(Encoding.UTF8.GetBytes(assetPath));
                }

                string entryPath = Path.Combine(saveFilePath, entry.Name);
                if (entryPath.EndsWith('/'))
                {
                    Directory.CreateDirectory(entryPath);
                }
                else
                {
                    entry.DataStream ??= new MemoryStream();
                    Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);
                    await using Stream entryStream = File.Create(entryPath);
                    await entry.DataStream.CopyToAsync(entryStream);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"An error occurred while processing entry: '{entry.Name}'.", ex);
            }

            processedEntries++;
            int currentProgress = 10 + (int)(80.0 * processedEntries / totalEntries);

            if (currentProgress != lastProgress)
            {
                if (reportProgress != null) await reportProgress.Invoke((Loc.Processing.Unitypackage.Status.Extracting, currentProgress));
                lastProgress = currentProgress;
            }
            
            return true;
        });

        return processedEntries;
    }

    private static async Task TarGzReader(string filePath, Func<TarEntry, Task<bool>> action)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        await using var tarReader = new TarReader(gzipStream);

        while (await tarReader.GetNextEntryAsync() is { } entry)
        {
            var shouldContinue = await action(entry);
            if (!shouldContinue) break;
        }
    }
    private static async Task TarGzReader(string filePath, Action<TarEntry> action)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        await using var tarReader = new TarReader(gzipStream);

        while (await tarReader.GetNextEntryAsync() is { } entry)
        {
            action(entry);
        }
    }
    private static async Task CreateTarArchive(string sourceFolder, string outputTarFile)
    {
        if (!Directory.Exists(sourceFolder)) throw new DirectoryNotFoundException(sourceFolder);

        await using var archive = await TarArchive.CreateAsyncArchive();

        foreach (var filePath in EnumerateFiles(sourceFolder))
        {
            var relativePath = Path.GetRelativePath(sourceFolder, filePath);
            await archive.AddEntryAsync(relativePath, filePath);
        }

        await using FileStream fileStream = new(outputTarFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1024 * 1024, FileOptions.SequentialScan);
        await archive.SaveToAsync(fileStream, new TarWriterOptions(CompressionType.None));
    }
    #endregion

    #region Extract Item Folders
    internal static async Task<ErrorOr<ExtractResult>> ExtractItemPaths(string parentFolderPath, IEnumerable<ItemPathEntry> itemPaths, bool shouldLinkToOriginal, int maxDegreeOfParallelism = 4, bool removeOriginal = false)
    {
        var result = new ExtractResult();

        foreach (var itemPath in itemPaths)
        {
            var targetPath = itemPath.Path;

            if (itemPath.IsUrl)
            {
                var downloadedPath = Path.Combine(GetNewTempFolder(), Path.GetFileName(itemPath.FileName));

                var downloadResult = await Downloader.Fetch(targetPath, downloadedPath);
                if (!downloadResult)
                {
                    ErrorManager.Instance.PostInternalError($"An error occurred while downloading item '{targetPath}'.");
                    result.ProcessingFailedPaths.Add(downloadedPath);
                    continue;
                }
                targetPath = downloadedPath;
            }

            var extractResult = await ExtractItemInternalAsync(
                targetPath,
                parentFolderPath,
                removeOriginal
            );

            if (extractResult.IsError)
            {
                ErrorManager.Instance.PostInternalError($"An error occurred while processing folder '{targetPath}'.", tag: extractResult.Errors.ToErrorString());
                result.ProcessingFailedPaths.Add(targetPath);
            }

            if (!string.IsNullOrEmpty(extractResult.Value.ExtractedFolderPath))
            {
                // 展開されたら、使用されたということ
                if (string.IsNullOrEmpty(result.ItemParentFolder))
                    result.ItemParentFolder = parentFolderPath;
            }
            else if (extractResult.Value.IsDirectory)
            {
                if (shouldLinkToOriginal)
                {
                    result.FolderPaths.Add(targetPath);
                    continue;
                }
                else
                {
                    var copiedFolderPath = GetUniquePath(parentFolderPath, Path.GetFileName(targetPath), true);
                    var copyResult = await CopyDirectoryAsync(targetPath, copiedFolderPath, maxDegreeOfParallelism);
                    if (copyResult.IsError)
                    {
                        ErrorManager.Instance.PostInternalError($"Failed to copy directory: {targetPath}");
                        result.ProcessingFailedPaths.Add(targetPath);
                        continue;
                    }

                    if (copyResult.Value.Failures.Count > 0)
                    {
                        copyResult.Value.Failures.ForEach(i => ErrorManager.Instance.PostInternalError($"Failed to copy: {i.SourcePath}", tag: i.ErrorMessage));
                    }
                    
                    result.ItemParentFolder = copiedFolderPath;
                }
            }
        }

        return result;
    }
    private static async Task<ErrorOr<FileExtractResultInternal>> ExtractItemInternalAsync(string filePath, string destinationFolderPath, bool removeOriginal)
    {
        var extractResult = await FileExtractorInternalAsync(filePath, destinationFolderPath, removeOriginal);
        if (extractResult.IsError) return Error.Failure(description: "Failed to process file.");

        return extractResult.Value;
    }
    #endregion

    #region Extractor
    private sealed class FileExtractResultInternal
    {
        public bool IsDirectory { get; set; } = false;
        public string ExtractedFolderPath { get; set; } = string.Empty;
    }
    private static async Task<ErrorOr<FileExtractResultInternal>> FileExtractorInternalAsync(string filePath, string extractDirectory, bool removeOriginalFile)
    {
        var extractResult = new FileExtractResultInternal();

        if (Directory.Exists(filePath))
        {
            extractResult.IsDirectory = true;
            return extractResult;
        }

        var extractDirectoryFolderPath = GetUniquePath(extractDirectory, Path.GetFileNameWithoutExtension(filePath), true);

        var extension = Path.GetExtension(filePath).ToLower();

        Func<string?, Task> extractAction = extension switch
        {
            ".zip" => password => ZipExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".rar" => password => RarExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".7z" => password => SevenZipExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".gz" => _ => GzipExtractorAsync(filePath, extractDirectoryFolderPath),
            ".tar" => _ => TarExtractorAsync(filePath, extractDirectoryFolderPath),
            _ => _ => CopyFileAsync(filePath, Path.Combine(extractDirectoryFolderPath, Path.GetFileName(filePath)))
        };

        var extractArchiveResult = await ExtractArchiveWithPasswordAsync(filePath, extractAction);
        if (extractArchiveResult.IsError) return Error.Failure(description: $"Failed to extract archive: '{filePath}'.");

        if (removeOriginalFile)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"Failed to remove original file: '{filePath}'.", ex);
            }
        }

        extractResult.ExtractedFolderPath = extractDirectoryFolderPath;

        return extractResult;
    }

    private const int MaxPasswordAttempts = 3;
    private static async Task<ErrorOr<Success>> ExtractArchiveWithPasswordAsync(string archivePath, Func<string?, Task> extractAction)
    {
        string? password = null;

        for (int attempt = 1; attempt <= MaxPasswordAttempts; attempt++)
        {
            try
            {
                await extractAction(password);
                return Result.Success;
            }
            catch (Exception ex) when (IsPasswordRelatedException(ex))
            {
                var passwordProvider = AvatarExplorerApp.Instance.ArchivePasswordProvider;
                if (passwordProvider == null) throw;

                var result = await passwordProvider.Invoke(new ArchivePasswordRequest
                {
                    ArchivePath = archivePath,
                    MaxAttempts = MaxPasswordAttempts,
                    Attempt = attempt,
                    ErrorMessage = ex.Message
                });

                if (result == null) return Error.Failure(description: "Password input cancelled by user.");

                password = result;
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"An error occurred while extracting archive: '{archivePath}'.", ex);
                return Error.Failure(description: $"An error occurred while extracting archive: '{archivePath}'.");
            }
        }

        return Error.Failure(description: $"Failed to extract archive after multiple attempts. Archive: '{archivePath}'.");
    }

    private static bool IsPasswordRelatedException(Exception ex)
    {
        if (ex is CryptographicException) return true;

        var current = ex;
        while (current != null)
        {
            var message = current.Message.ToLowerInvariant();
            if (message.Contains("password") || message.Contains("encrypted") || message.Contains("passphrase") || message.Contains("decrypt"))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private static async Task ZipExtractorAsync(string filePath, string extractDirectoryFolder, string? password = null)
    {
        using var archive = SharpCompress.Archives.Zip.ZipArchive.OpenArchive(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task RarExtractorAsync(string filePath, string extractDirectoryFolder, string? password = null)
    {
        using var archive = SharpCompress.Archives.Rar.RarArchive.OpenArchive(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task SevenZipExtractorAsync(string filePath, string extractDirectoryFolder, string? password = null)
    {
        using var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.OpenArchive(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task GzipExtractorAsync(string filePath, string extractDirectoryFolder)
    {
        using var archive = SharpCompress.Archives.GZip.GZipArchive.OpenArchive(filePath);
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task TarExtractorAsync(string filePath, string extractDirectoryFolder)
    {
        using var archive = SharpCompress.Archives.Tar.TarArchive.OpenArchive(filePath);
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static SharpCompress.Readers.ReaderOptions CreateReaderOptions(string? password)
    {
        return new()
        {
            Password = string.IsNullOrEmpty(password) ? null : password,
            LeaveStreamOpen = false
        };
    }
    private static async Task ExtractEntriesAsync<T>(string extractDirectoryFolder, IEnumerable<T> entries)
        where T : IEntry, IArchiveEntry
    {
        var buffer = new byte[BufferSize];

        foreach (var entry in entries)
        {
            if (!entry.IsDirectory)
            {
                string fullPath = Path.Combine(extractDirectoryFolder, entry.Key!);
                PrepareFileDirectory(fullPath);

                await using var inStream = await entry.OpenEntryStreamAsync();
                await using var outStream = File.Create(fullPath);

                int read;
                while ((read = await inStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await outStream.WriteAsync(buffer.AsMemory(0, read));
                }
            }
            else if (entry.Key != null)
            {
                Directory.CreateDirectory(Path.Combine(extractDirectoryFolder, entry.Key));
            }
        }
    }
    #endregion

    #region Copy
    public async static Task<ErrorOr<CopyResult>> CopyDirectoryAsync(string sourceDirectory, string destinationDirectory, int maxDegreeOfParallelism, Func<(string LocalizationKey, int), Task>? reportProgress = null)
    {
        if (sourceDirectory == destinationDirectory)
            return Error.Conflict("Directory.Copy", "Source and destination are the same.");

        if (!Directory.Exists(sourceDirectory))
            return Error.NotFound("Directory.Copy", $"Source directory not found: {sourceDirectory}");

        try
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to create destination directory.", ex);
            return Error.Failure("Directory.Create", "Failed to create destination directory.");
        }

        IEnumerable<string> allFiles;

        try
        {
            allFiles = EnumerateFiles(sourceDirectory);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to enumerate files.", ex);
            return Error.Failure("Directory.Enumerate", $"Failed to enumerate files.");
        }

        var fileList = allFiles.ToList();
        int totalFiles = fileList.Count;

        if (totalFiles == 0) return new CopyResult { SuccessCount = 0, TotalCount = 0 };

        int copiedFiles = 0;
        int lastReportedPercent = -1;
        var failures = new ConcurrentBag<CopyFailure>();

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = fileList.Select(async file =>
        {
            await semaphore.WaitAsync();
            try
            {
                string relativePath = Path.GetRelativePath(sourceDirectory, file);
                string destPath = Path.Combine(destinationDirectory, relativePath);

                // CopyFileAsyncの結果を確認
                var copyResult = await CopyFileAsync(file, destPath);
                
                if (copyResult.IsError)
                {
                    failures.Add(new CopyFailure
                    {
                        SourcePath = file,
                        DestinationPath = destPath,
                        ErrorMessage = copyResult.Errors.ToErrorString()
                    });
                }
            }
            finally
            {
                int current = Interlocked.Increment(ref copiedFiles);
                int percent = (int)(current / (double)totalFiles * 100);
                
                if (percent != Volatile.Read(ref lastReportedPercent) && reportProgress != null)
                {
                    int previous = Interlocked.CompareExchange(ref lastReportedPercent, percent, lastReportedPercent);
                    if (previous != percent)
                    {
                        try
                        {
                            await reportProgress.Invoke((Loc.Processing.DirectoryCopy.Copying, percent));
                        }
                        catch
                        {
                            // Ignored
                        }
                    }
                }

                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return new CopyResult
        {
            SuccessCount = totalFiles - failures.Count,
            TotalCount = totalFiles,
            Failures = failures.ToList()
        };
    }

    public static async Task<ErrorOr<Success>> CopyFileAsync(string sourceFile, string destinationFile)
    {
        try
        {
            if (sourceFile == destinationFile)
                return Error.Conflict(description: "Source and destination are the same.");

            if (!File.Exists(sourceFile))
                return Error.NotFound(description:$"Source file not found: '{sourceFile}'.");

            PrepareFileDirectory(destinationFile);
            await using var sourceStream = File.OpenRead(sourceFile);
            await using var destStream = File.Create(destinationFile);
            await sourceStream.CopyToAsync(destStream, BufferSize);

            return Result.Success;
        }
        catch (Exception ex)
        {
            return Error.Failure(description: $"Failed to copy file: '{ex.Message}'.");
        }
    }
    #endregion

    public static void PrepareFileDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? filePath;
        Directory.CreateDirectory(directory);
    }

    public static IEnumerable<string> EnumerateFiles(string rootDirectory, bool isRecursive = true)
    {
        if (!Directory.Exists(rootDirectory))
        {
            ErrorManager.Instance.PostInternalError($"Directory not found: {rootDirectory}.");
            yield break;
        }

        var directories = new Stack<string>();
        directories.Push(rootDirectory);

        while (directories.Count > 0)
        {
            var directory = directories.Pop();

            string[] subDirectories;

            try
            {
                subDirectories = Directory.GetDirectories(directory);
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"Failed to retrieve subdirectories for '{directory}'.", ex);
                continue;
            }

            if (isRecursive)
            {
                foreach (var subDirectory in subDirectories)
                {
                    directories.Push(subDirectory);
                }
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception ex)
            {
                ErrorManager.Instance.PostInternalError($"Failed to retrieve files for '{directory}'.", ex);
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    public static string GetUniquePath(string directory, string fileName, bool isDirectory = false)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        var path = Path.Combine(directory, fileName);
        if ((!isDirectory && !File.Exists(path)) || (isDirectory && !Directory.Exists(path))) return path;

        var index = 1;

        while (true)
        {
            var newName = $"{fileNameWithoutExtension} - {index}{extension}";
            var newPath = Path.Combine(directory, newName);

            if ((!isDirectory && !File.Exists(newPath)) || (isDirectory && !Directory.Exists(newPath)))
                return newPath;

            index++;
        }
    }

    public static bool DeleteDirectory(string path, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(path)) return false;
            Directory.Delete(path, recursive);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
