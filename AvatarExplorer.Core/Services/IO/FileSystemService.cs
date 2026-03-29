using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers;

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
    public List<string> ProcessingFailedPaths { get; init; } = new();
}

public class ModifiedUnitypackagesResult
{
    public bool IsError { get; set; } = true;
    public string? ModifiedUnitypackagePath { get; set; } = null;
    public List<string> Success { get; } = new();
    public List<string> Failed { get; } = new();
}

public static class FileSystemService
{
    private const int BufferSize = 1024 * 1024;

    #region Serialize / Deserialize
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    public static ErrorOr<Success> SerializeClass<T>(T value, string filePath)
    {
        try
        {
            PrepareFileDirectory(filePath);
            string json = JsonSerializer.Serialize(value, JsonSerializerOptions);
            File.WriteAllText(filePath, json);
            return Result.Success;
        }
        catch (Exception ex)
        {
            Type elementType = typeof(T).GetGenericArguments().FirstOrDefault() ?? typeof(T);
            ErrorManager.Instance.PostInternalError($"Failed to serialize class: '{elementType.Name}' to '{filePath}'.", ex);
            return Error.Failure(description: "Failed to serialize class.");
        }
    }
    public static ErrorOr<T> DeserializeClass<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return Error.NotFound(description: $"File not found: {filePath}");
            
            string json = File.ReadAllText(filePath);
            T? result = JsonSerializer.Deserialize<T>(json);
            
            if (Equals(result, default(T))) return Error.Failure(description: "deserialization result is null.");
            
            return result!;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to desearize class: '{typeof(T).Name}' from '{filePath}'.", ex);
            return Error.Failure(description: "Failed to desearize class.");
        }
    }
    #endregion

    #region Unitypackage Modifier
    internal static async Task<ModifiedUnitypackagesResult> ModifyUnitypackageFilePathsAsync(Dictionary<string, string> itemPathCategoryDictionary, Func<(string, int), Task>? reportProgress = null)
    {
        ModifiedUnitypackagesResult result = new();

        try
        {
            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Unitypackage.Status.Preparing, 0));

            string saveFolderPath = PrepareSaveFolderPath();
            string unitypackagePath = saveFolderPath + ".unitypackage";

            if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Unitypackage.Status.Extracting, 10));

            int totalEntries = 0;
            foreach (string itemPath in itemPathCategoryDictionary.Keys)
            {
                try
                {
                    totalEntries += await CountTarEntriesAsync(itemPath);
                }
                catch
                {
                    itemPathCategoryDictionary.Remove(itemPath); // 事前に処理に失敗する可能性があるものは削除しておく
                }
            }

            int currentProcessedEntries = 0;
            foreach (var itemPathCategoryKpv in itemPathCategoryDictionary)
            {
                try
                {
                    currentProcessedEntries = await ExtractTarToFolderAsync(itemPathCategoryKpv.Key, saveFolderPath, itemPathCategoryKpv.Value, totalEntries, currentProcessedEntries, reportProgress);
                    result.Success.Add(itemPathCategoryKpv.Key);
                }
                catch
                {
                    result.Failed.Add(itemPathCategoryKpv.Key);
                }
            }

            reportProgress?.Invoke((LocalizationKey.Processing.Unitypackage.Status.Creating, 90));

            await CreateTarArchive(saveFolderPath, unitypackagePath);

            DeleteDirectory(saveFolderPath, true);

            reportProgress?.Invoke((LocalizationKey.Processing.Unitypackage.Status.Completed, 100));

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
        static string getNextFolder(string basePath)
        {
            int i = 1;
            while (Directory.Exists(Path.Combine(basePath, i.ToString()))) i++;
            return Path.Combine(basePath, i.ToString());
        }

        string saveFolderPath = Path.Combine(getNextFolder(SystemPath.TempFolderPath), "Unitypackages Modified by Avatar Explorer");
        Directory.CreateDirectory(saveFolderPath);

        return saveFolderPath;
    }
    private static async Task<int> CountTarEntriesAsync(string tarGzFilePath)
    {
        // これ下と同じだから、Actionとかで統一化しても良いかも
        int count = 0;
        await using Stream fileStream = File.OpenRead(tarGzFilePath);
        await using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress);
        await using TarReader tarReader = new(gzipStream);
        while (await tarReader.GetNextEntryAsync() is { })
            count++;
        return count;
    }
    private static async Task<int> ExtractTarToFolderAsync(string tarGzFilePath, string saveFilePath, string category, int totalEntries, int currentProcessedEntries = 0, Func<(string, int), Task>? reportProgress = null)
    {
        int processedEntries = currentProcessedEntries;

        await using Stream fileStream = File.OpenRead(tarGzFilePath);
        await using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress);
        await using TarReader tarReader = new(gzipStream);

        int lastProgress = -1;

        while (await tarReader.GetNextEntryAsync() is { } entry)
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
                if (reportProgress != null) await reportProgress.Invoke((LocalizationKey.Processing.Unitypackage.Status.Extracting, currentProgress));
                lastProgress = currentProgress;
            }
        }

        return processedEntries;
    }
    private static async Task CreateTarArchive(string sourceFolder, string outputTarFile)
    {
        if (!Directory.Exists(sourceFolder)) throw new DirectoryNotFoundException(sourceFolder);

        using TarArchive archive = TarArchive.Create();

        foreach (string filePath in EnumerateFiles(sourceFolder))
        {
            string relativePath = Path.GetRelativePath(sourceFolder, filePath);
            archive.AddEntry(relativePath, filePath);
        }

        using FileStream fileStream = new(outputTarFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1024 * 1024, FileOptions.SequentialScan);
        await archive.SaveToAsync(fileStream, new WriterOptions(CompressionType.None));
    }
    #endregion

    #region Extract Item Folders
    internal static async Task<ErrorOr<ExtractResult>> ExtractItemFolders(ItemCreationContext itemCreationContext, string dataRootDirectory, RuntimeSettings runtimeSettings)
    {
        if (itemCreationContext.ItemPaths.Count == 0) return Error.Failure(description: "No item paths provided.");

        ExtractResult result = new();

        try
        {
            bool linkedToOriginal = false;

            string parentFolder;
            if (runtimeSettings.ShouldLinkToOriginal && Directory.Exists(itemCreationContext.ItemPaths[0]))
            {
                parentFolder = itemCreationContext.ItemPaths[0];
                linkedToOriginal = true;
            }
            else
            {
                parentFolder = GetUniquePath(dataRootDirectory, ItemUtils.GetSafeTitle(itemCreationContext.Title) ?? Path.GetFileNameWithoutExtension(itemCreationContext.ItemPaths[0]), true);
            }
            
            Directory.CreateDirectory(parentFolder);

            foreach (string itemPath in linkedToOriginal ? itemCreationContext.ItemPaths.Skip(1) : itemCreationContext.ItemPaths)
            {
                ErrorOr<Success> extractResult = await ExtractItemInternalAsync(
                    itemPath,
                    parentFolder,
                    runtimeSettings
                );

                if (extractResult.IsError)
                {
                    ErrorManager.Instance.PostInternalError($"An error occurred while processing folder '{itemPath}'.", tag: extractResult.Errors.ToErrorString());
                    result.ProcessingFailedPaths.Add(itemPath);
                }
            }

            result.ItemParentFolder = linkedToOriginal ? parentFolder : $"<sys>{Path.GetRelativePath(dataRootDirectory, parentFolder)}";

            return result;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to extract item.", ex);
            return Error.Failure(description: "Failed to extract item.");
        }
    }
    internal static async Task<ExtractResult> ExtractItemPaths(string parentFolderPath, string[] itemPaths, RuntimeSettings runtimeSettings)
    {
        ExtractResult result = new();

        foreach (string itemPath in itemPaths)
        {
            ErrorOr<Success> extractResult = await ExtractItemInternalAsync(
                itemPath,
                parentFolderPath,
                runtimeSettings
            );

            if (extractResult.IsError)
            {
                ErrorManager.Instance.PostInternalError($"An error occurred while processing folder '{itemPath}'.", tag: extractResult.Errors.ToErrorString());
                result.ProcessingFailedPaths.Add(itemPath);
            }
        }

        return result;
    }
    private static async Task<ErrorOr<Success>> ExtractItemInternalAsync(string filePath, string destinationFolderPath, RuntimeSettings runtimeSettings)
    {
        ErrorOr<FileExtractResultInternal> extractResult = await FileExtractorInternalAsync(filePath, destinationFolderPath, runtimeSettings.RemoveOriginal);

        if (extractResult.IsError)
        {
            return Error.Failure(description: "Failed to process file.");
        }

        if (extractResult.Value.IsDirectory)
        {
            string copiedFolderPath = GetUniquePath(destinationFolderPath, Path.GetFileNameWithoutExtension(filePath), true);
            ErrorOr<CopyResult> copyResult = await CopyDirectoryAsync(filePath, copiedFolderPath, runtimeSettings.MaxDegreeOfParallelism);

            if (copyResult.IsError)
            {
                return Error.Failure(description: "Failed to copy directory.");
            }

            if (copyResult.Value.Failures.Count > 0)
            {
                copyResult.Value.Failures.ForEach(i => ErrorManager.Instance.PostInternalError($"Failed to copy: {i.SourcePath}", tag: i.ErrorMessage));
            }
        }

        return Result.Success;
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
        FileExtractResultInternal extractResult = new();

        if (Directory.Exists(filePath))
        {
            extractResult.IsDirectory = true;
            return extractResult;
        }

        string extractDirectoryFolderPath = GetUniquePath(extractDirectory, Path.GetFileNameWithoutExtension(filePath), true);

        string extension = Path.GetExtension(filePath).ToLower();

        Func<string?, Task> extractAction = extension switch
        {
            ".zip" => password => ZipExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".rar" => password => RarExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".7z" => password => SevenZipExtractorAsync(filePath, extractDirectoryFolderPath, password),
            ".gz" => _ => GzipExtractorAsync(filePath, extractDirectoryFolderPath),
            ".tar" => _ => TarExtractorAsync(filePath, extractDirectoryFolderPath),
            _ => _ => CopyFileAsync(filePath, Path.Combine(extractDirectoryFolderPath, Path.GetFileName(filePath)))
        };

        ErrorOr<Success> extractArchiveResult = await ExtractArchiveWithPasswordAsync(filePath, extractAction);
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
                Func<ArchivePasswordRequest, ValueTask<string?>>? passwordProvider = AvatarExplorerApp.Instance.PasswordProvider;
                if (passwordProvider == null) throw;

                string? result = await passwordProvider.Invoke(new ArchivePasswordRequest
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

        Exception? current = ex;
        while (current != null)
        {
            string message = current.Message.ToLowerInvariant();
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
        using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task RarExtractorAsync(string filePath, string extractDirectoryFolder, string? password = null)
    {
        using var archive = SharpCompress.Archives.Rar.RarArchive.Open(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task SevenZipExtractorAsync(string filePath, string extractDirectoryFolder, string? password = null)
    {
        using var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(filePath, CreateReaderOptions(password));
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task GzipExtractorAsync(string filePath, string extractDirectoryFolder)
    {
        using var archive = SharpCompress.Archives.GZip.GZipArchive.Open(filePath);
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static async Task TarExtractorAsync(string filePath, string extractDirectoryFolder)
    {
        using var archive = SharpCompress.Archives.Tar.TarArchive.Open(filePath);
        await ExtractEntriesAsync(extractDirectoryFolder, archive.Entries);
    }
    private static SharpCompress.Readers.ReaderOptions CreateReaderOptions(string? password)
    {
        SharpCompress.Readers.ReaderOptions options = new();
        if (!string.IsNullOrEmpty(password)) options.Password = password;
        return options;
    }
    private static async Task ExtractEntriesAsync<T>(string extractDirectoryFolder, ICollection<T> entries)
        where T : Entry, IArchiveEntry
    {
        byte[] buffer = new byte[BufferSize];

        foreach (T entry in entries)
        {
            if (!entry.IsDirectory)
            {
                string fullPath = Path.Combine(extractDirectoryFolder, entry.Key!);
                PrepareFileDirectory(fullPath);

                using Stream inStream = await entry.OpenEntryStreamAsync();
                using Stream outStream = File.Create(fullPath);

                int read;
                while ((read = await inStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await outStream.WriteAsync(buffer, 0, read);
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
        
        try
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to create destination directory.", ex);
            return Error.Failure("Directory.Create", "Failed to create destination directory.");
        }

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
                            await reportProgress.Invoke((LocalizationKey.Processing.DirectoryCopy.Copying, percent));
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
            using Stream sourceStream = File.OpenRead(sourceFile);
            using Stream destStream = File.Create(destinationFile);
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
        string directory = Path.GetDirectoryName(filePath) ?? filePath;
        Directory.CreateDirectory(directory);
    }

    public static IEnumerable<string> EnumerateFiles(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory)) throw new DirectoryNotFoundException($"Directory not found: {rootDirectory}.");

        Stack<string> directories = new();
        directories.Push(rootDirectory);

        while (directories.Count > 0)
        {
            string directory = directories.Pop();

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

            foreach (string subDirectory in subDirectories)
            {
                directories.Push(subDirectory);
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

            foreach (string file in files)
            {
                yield return file;
            }
        }
    }

    public static string GetUniquePath(string directory, string fileName, bool isDirectory = false)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        string path = Path.Combine(directory, fileName);
        if ((!isDirectory && !File.Exists(path)) || (isDirectory && !Directory.Exists(path))) return path;

        int index = 1;

        while (true)
        {
            string newName = $"{fileNameWithoutExtension} - {index}{extension}";
            string newPath = Path.Combine(directory, newName);

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
