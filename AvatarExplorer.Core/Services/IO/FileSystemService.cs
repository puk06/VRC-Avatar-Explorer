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

/// <summary>
/// ディレクトリのコピー処理の結果を保持するレコードです。
/// </summary>
public record CopyResult
{
    /// <summary>コピーに成功したファイルの数を取得します。</summary>
    public int SuccessCount { get; init; }
    /// <summary>コピー対象となったファイルの総数を取得します。</summary>
    public int TotalCount { get; init; }
    /// <summary>コピーに失敗したファイルの情報一覧を取得します。</summary>
    public List<CopyFailure> Failures { get; init; } = [];
}
/// <summary>
/// コピーに失敗した個々のファイルの情報を保持するレコードです。
/// </summary>
public record CopyFailure
{
    /// <summary>コピー元のファイルパスを取得します。</summary>
    public string SourcePath { get; init; } = string.Empty;
    /// <summary>コピー先のファイルパスを取得します。</summary>
    public string DestinationPath { get; init; } = string.Empty;
    /// <summary>失敗の原因となったエラーメッセージを取得します。</summary>
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// アイテムのパス（ファイル・フォルダ・URL）を展開・抽出した結果を保持するレコードです。
/// </summary>
public record ExtractResult
{
    /// <summary>展開されたアイテムの親フォルダパスを取得します。未展開の場合は空文字列です。</summary>
    public string ItemParentFolder { get; set; } = string.Empty;
    /// <summary>リンクとしてそのまま追加された（展開されなかった）フォルダパス一覧を取得します。</summary>
    public List<string> FolderPaths { get; } = [];
    /// <summary>処理に失敗したパス一覧を取得します。</summary>
    public List<string> ProcessingFailedPaths { get; init; } = [];
    internal Lock SyncRoot { get; } = new();
}

/// <summary>
/// Unitypackageのパス変更・複数統合処理の結果を保持するクラスです。
/// </summary>
public class ModifiedUnitypackagesResult
{
    /// <summary>処理中にエラーが発生したかどうかを取得または設定します。</summary>
    public bool IsError { get; set; } = false;
    /// <summary>作成された（変更・統合後の）Unitypackageのパスを取得または設定します。失敗時は null です。</summary>
    public string? ModifiedUnitypackagePath { get; set; } = null;
    /// <summary>処理に成功した入力ファイル（Unitypackage）のパス一覧を取得します。</summary>
    public List<string> Success { get; } = [];
    /// <summary>処理に失敗した入力ファイル（Unitypackage）のパス一覧を取得します。</summary>
    public List<string> Failed { get; } = [];
    /// <summary>パッケージ内に C# スクリプト (.cs) が含まれているかどうかを取得または設定します。</summary>
    public bool ContainsScripts { get; set; }
    /// <summary>元のファイルと同一で変更が行われなかったかどうかを取得または設定します。</summary>
    public bool IsNotModified { get; set; } = false;
}

/// <summary>
/// アイテムに追加するファイルまたはフォルダのパス情報を保持するクラスです。
/// </summary>
public class ItemPathEntry
{
    /// <summary>ファイルまたはフォルダの名前を取得または設定します。</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>ファイルまたはフォルダのパスを取得または設定します。リモートの場合は URL を指定します。</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary><see cref="Path"/> が URL（リモート）かどうかを取得または設定します。</summary>
    public bool IsUrl { get; set; } = false;
}

/// <summary>
/// ファイルシステム関連のユーティリティを提供する静的クラスです。JSON のシリアライズ、Unitypackage のパス変更・抽出、アーカイブの展開、ディレクトリ・ファイルのコピーなどを行います。
/// </summary>
public static class FileSystemService
{
    private const int BufferSize = 1024 * 1024;

    #region Serialize / Deserialize
    /// <summary>
    /// 指定したオブジェクトを JSON にシリアライズし、指定したファイルパスに書き込みます。
    /// </summary>
    /// <typeparam name="T">シリアライズするオブジェクトの型（参照型）。</typeparam>
    /// <param name="value">シリアライズするオブジェクト。</param>
    /// <param name="filePath">書き込み先のファイルパス。</param>
    /// <returns>成功した場合は <see cref="Success"/>、失敗した場合はエラーを返します。</returns>
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
    /// <summary>
    /// 指定した JSON ファイルを読み込み、型 <typeparamref name="T"/> のオブジェクトにデシリアライズします。
    /// </summary>
    /// <typeparam name="T">デシリアライズ先の型（参照型）。</typeparam>
    /// <param name="filePath">読み込む JSON ファイルのパス。</param>
    /// <returns>成功した場合はデシリアライズされたオブジェクト、ファイルが存在しない・失敗した場合はエラーを返します。</returns>
    public static ErrorOr<T> DeserializeClass<T>(string filePath) where T : class
    {
        try
        {
            if (!File.Exists(filePath)) return Error.NotFound(description: $"File not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var result = JsonManager.Deserialize<T>(json);

            return result ?? (ErrorOr<T>)Error.Failure(description: "deserialization result is null.");
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to desearize class: '{typeof(T).Name}' from '{filePath}'.", ex);
            return Error.Failure(description: "Failed to desearize class.");
        }
    }
    #endregion

    #region Unitypackage Modifier
    /// <summary>
    /// 指定した Unitypackage に含まれる pathname エントリ（アセットのインポートパス）の一覧を取得します。
    /// </summary>
    /// <param name="unitypackagePath">対象の Unitypackage ファイルのパス。</param>
    /// <returns>pathname の一覧。ファイルが存在しない・失敗した場合はエラーを返します。</returns>
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

    /// <summary>
    /// 指定した Unitypackage から、特定の pathname に対応するアセット（ファイル）を抽出し、指定したフォルダに展開します。
    /// </summary>
    /// <param name="unitypackagePath">対象の Unitypackage ファイルのパス。</param>
    /// <param name="pathname">抽出対象のアセットの pathname（Assets/... 形式）。</param>
    /// <param name="destinationFolderPath">抽出先のフォルダパス。</param>
    /// <returns>抽出されたファイルのフルパス。該当エントリが見つからない・失敗した場合はエラーを返します。</returns>
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

        var extracted = false;

        try
        {
            await TarGzReader(unitypackagePath, async entry =>
            {
                if (entry.DataStream == null) return true;
                if (!string.Equals(GetUnitypackageTopLevelFolder(entry.Name), targetGroupFolder, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.Equals(Path.GetFileName(entry.Name), "asset", StringComparison.OrdinalIgnoreCase)) return true;

                await using var outputStream = File.Create(extractedFilePath);
                await entry.DataStream.CopyToAsync(outputStream);

                extracted = true;
                return false;
            });
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to export asset from unitypackage: '{unitypackagePath}'.", ex);
            return Error.Failure(description: "Failed to export asset from unitypackage.");
        }

        return extracted ? extractedFilePath : Error.NotFound(description: $"Asset entry not found for pathname: '{pathname}'.");
    }

    private static string NormalizeUnitypackagePath(string path) => path.Trim().Replace('\\', '/');

    private static string GetUnitypackageTopLevelFolder(string entryName)
    {
        var normalizedEntryName = NormalizeUnitypackagePath(entryName);
        int separatorIndex = normalizedEntryName.IndexOf('/');
        return separatorIndex >= 0 ? normalizedEntryName[..separatorIndex] : normalizedEntryName;
    }

    /// <summary>
    /// Unitypackage のインポートパスを変更（カテゴリ名の挿入）する、または複数の Unitypackage を1つに統合する処理を行い、新しい Unitypackage を作成します。
    /// </summary>
    /// <param name="request">処理内容（入力エントリ・パス変更の有無・進捗コールバックなど）を指定するリクエスト。</param>
    /// <returns>処理結果（作成されたパッケージのパスや成功・失敗一覧など）を保持する <see cref="ModifiedUnitypackagesResult"/>。</returns>
    public static async Task<ModifiedUnitypackagesResult> ModifyUnitypackageFilePathsAsync(UnitypackageModifyRequest request)
    {
        var result = new ModifiedUnitypackagesResult();

        var entries = request.Entries;
        var changeUnitypackagePath = request.ChangeUnitypackagePath ?? AvatarExplorerApp.Instance.RuntimeSettings.AutoChangeUnitypackagePath;
        var reportProgress = request.ReportProgress;

        if (entries.Count == 1 && !changeUnitypackagePath)
        {
            // 処理したとしても元のUnitypackageと同じのため、そのまま返してあげる
            result.ModifiedUnitypackagePath = entries[0].FilePath;
            result.Success.Add(entries[0].FilePath);
            result.IsNotModified = true;
            return result;
        }

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
                    var (ProcessedEntries, ContainsScripts) = await ExtractUnitypackageToFolderAsync(entry.FilePath, saveFolderPath, entry.CategoryDisplayName, changeUnitypackagePath, totalEntries, currentProcessedEntries, reportProgress);
                    currentProcessedEntries = ProcessedEntries;
                    if (ContainsScripts) result.ContainsScripts = true;
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
    /// <summary>
    /// 一時フォルダ内に新しいユニークなフォルダを作成し、そのパスを返します。
    /// </summary>
    /// <returns>新しく作成された一時フォルダのパス。</returns>
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
    private static async Task<(int ProcessedEntries, bool ContainsScripts)> ExtractUnitypackageToFolderAsync(string tarGzFilePath, string saveFilePath, string category, bool changeUnitypackagePath, int totalEntries, int currentProcessedEntries = 0, Func<(string Message, int Percent), Task>? reportProgress = null)
    {
        int processedEntries = currentProcessedEntries;
        bool containsScripts = false;

        int lastProgress = -1;

        await TarGzReader(tarGzFilePath, async entry =>
        {
            try
            {
                if (changeUnitypackagePath && Path.GetFileName(entry.Name) == "pathname" && entry.DataStream != null)
                {
                    using StreamReader reader = new(entry.DataStream);
                    string assetPath = await reader.ReadToEndAsync();

                    if (Path.GetExtension(assetPath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
                        containsScripts = true;

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

        return (processedEntries, containsScripts);
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
    private static DateTime _lastUrlFetchTime;
    private static readonly TimeSpan UrlCooldown = TimeSpan.FromSeconds(2);
    private static async Task WaitForUrlCooldownAsync(CancellationToken ct = default)
    {
        var delay = _lastUrlFetchTime + UrlCooldown - DateTime.Now;
        if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
    }

    internal static async Task<ErrorOr<ExtractResult>> ExtractItemPaths(string parentFolderPath, IEnumerable<ItemPathEntry> itemPaths, bool shouldLinkToOriginal, int maxDegreeOfParallelism = 4, bool removeOriginal = false)
    {
        var result = new ExtractResult();

        var urlEntries = new List<ItemPathEntry>();
        var fileEntries = new List<ItemPathEntry>();
        foreach (var entry in itemPaths)
        {
            if (entry.IsUrl) urlEntries.Add(entry);
            else fileEntries.Add(entry);
        }

        var urlTask = ProcessUrlEntriesAsync(result, urlEntries, parentFolderPath, shouldLinkToOriginal, removeOriginal, maxDegreeOfParallelism);
        var fileTask = ProcessFileEntriesAsync(result, fileEntries, parentFolderPath, shouldLinkToOriginal, removeOriginal, maxDegreeOfParallelism);

        await Task.WhenAll(urlTask, fileTask);

        return result;
    }

    private static async Task ProcessUrlEntriesAsync(ExtractResult result, List<ItemPathEntry> urlEntries, string parentFolderPath, bool shouldLinkToOriginal, bool removeOriginal, int maxDegreeOfParallelism)
    {
        foreach (var entry in urlEntries)
        {
            await WaitForUrlCooldownAsync();

            var url = entry.Path;
            var downloadedPath = Path.Combine(GetNewTempFolder(), Path.GetFileName(entry.FileName));

            _lastUrlFetchTime = DateTime.Now;
            var downloadResult = await Downloader.Fetch(url, downloadedPath);
            if (!downloadResult)
            {
                var host = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : "unknown";
                ErrorManager.Instance.PostInternalError($"Failed to download file from '{host}'.");
                lock (result.ProcessingFailedPaths) result.ProcessingFailedPaths.Add(downloadedPath);
                continue;
            }

            await ProcessExtractedPath(result, downloadedPath, parentFolderPath, shouldLinkToOriginal, removeOriginal, maxDegreeOfParallelism);
        }
    }

    private static async Task ProcessFileEntriesAsync(ExtractResult result, List<ItemPathEntry> fileEntries, string parentFolderPath, bool shouldLinkToOriginal, bool removeOriginal, int maxDegreeOfParallelism)
    {
        foreach (var entry in fileEntries)
        {
            await ProcessExtractedPath(result, entry.Path, parentFolderPath, shouldLinkToOriginal, removeOriginal, maxDegreeOfParallelism);
        }
    }

    private static async Task ProcessExtractedPath(ExtractResult result, string targetPath, string parentFolderPath, bool shouldLinkToOriginal, bool removeOriginal, int maxDegreeOfParallelism = 4)
    {
        var extractResult = await ExtractItemInternalAsync(targetPath, parentFolderPath, removeOriginal);

        if (extractResult.IsError)
        {
            lock (result.ProcessingFailedPaths) result.ProcessingFailedPaths.Add(targetPath);
            return;
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
                lock (result.FolderPaths) result.FolderPaths.Add(targetPath);
            }
            else
            {
                var copiedFolderPath = GetUniquePath(parentFolderPath, Path.GetFileName(targetPath), true);
                var copyResult = await CopyDirectoryAsync(targetPath, copiedFolderPath, maxDegreeOfParallelism);
                if (copyResult.IsError)
                {
                    ErrorManager.Instance.PostInternalError($"Failed to copy directory: {targetPath}");
                    lock (result.ProcessingFailedPaths) result.ProcessingFailedPaths.Add(targetPath);
                    return;
                }

                if (copyResult.Value.Failures.Count > 0)
                {
                    copyResult.Value.Failures.ForEach(i => ErrorManager.Instance.PostInternalError($"Failed to copy: {i.SourcePath}", tag: i.ErrorMessage));
                }

                lock (result.SyncRoot) result.ItemParentFolder = parentFolderPath;
            }
        }
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
    static ArchiveEncoding ArchiveEncoding
    {
        get
        {
            if (field is not null) return field;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            field = new ArchiveEncoding() { Default = new FallbackEncoding("Shift_JIS") };
            return field;
        }
    }
    private sealed class FallbackEncoding(string fallbackEncodingName) : Encoding
    {
        private readonly Encoding _utf8Strict = new UTF8Encoding(false, true);
        private readonly Encoding _fallback = GetEncoding(fallbackEncodingName);

        public override int GetByteCount(char[] chars, int index, int count) => _fallback.GetByteCount(chars, index, count);
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) => _fallback.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            try
            {
                return _utf8Strict.GetCharCount(bytes, index, count);
            }
            catch (DecoderFallbackException)
            {
                return _fallback.GetCharCount(bytes, index, count);
            }
        }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            try
            {
                return _utf8Strict.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
            catch (DecoderFallbackException)
            {
                return _fallback.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
        }
        public override int GetMaxByteCount(int charCount) => _fallback.GetMaxByteCount(charCount);
        public override int GetMaxCharCount(int byteCount) => _fallback.GetMaxCharCount(byteCount);
        public override Decoder GetDecoder() => new FallbackDecoder(_utf8Strict, _fallback);
        public override Encoder GetEncoder() => _fallback.GetEncoder();
        private sealed class FallbackDecoder(Encoding utf8Strict, Encoding fallback) : Decoder
        {
            private readonly Encoding _utf8Strict = utf8Strict;
            private readonly Encoding _fallback = fallback;
            private Decoder? _active = utf8Strict.GetDecoder();
            private bool _failed;

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                if (!_failed)
                {
                    try
                    {
                        return _utf8Strict.GetDecoder().GetCharCount(bytes, index, count);
                    }
                    catch (DecoderFallbackException)
                    {
                        _failed = true;
                        _active = _fallback.GetDecoder();
                        return _fallback.GetDecoder().GetCharCount(bytes, index, count);
                    }
                }
                return _active!.GetCharCount(bytes, index, count);
            }
            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                if (!_failed)
                {
                    try
                    {
                        return _utf8Strict.GetDecoder().GetChars(bytes, byteIndex, byteCount, chars, charIndex);
                    }
                    catch (DecoderFallbackException)
                    {
                        _failed = true;
                        _active = _fallback.GetDecoder();
                        return _fallback.GetDecoder().GetChars(bytes, byteIndex, byteCount, chars, charIndex);
                    }
                }
                return _active!.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
        }
    }
    private static SharpCompress.Readers.ReaderOptions CreateReaderOptions(string? password)
    {
        return new()
        {
            ArchiveEncoding = ArchiveEncoding,
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
                while ((read = await inStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
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
    /// <summary>
    /// ディレクトリを再帰的にコピーします。並列処理による高速化と、進捗報告のコールバックをサポートします。
    /// </summary>
    /// <param name="sourceDirectory">コピー元のディレクトリパス。</param>
    /// <param name="destinationDirectory">コピー先のディレクトリパス。</param>
    /// <param name="maxDegreeOfParallelism">並列コピーの最大同時実行数。</param>
    /// <param name="reportProgress">進捗を報告するコールバック（任意）。</param>
    /// <returns>コピー結果（成功数・失敗一覧など）を保持する <see cref="CopyResult"/>。引数が無効な場合はエラーを返します。</returns>
    public async static Task<ErrorOr<CopyResult>> CopyDirectoryAsync(string sourceDirectory, string destinationDirectory, int maxDegreeOfParallelism, Func<(string Message, int Percent), Task>? reportProgress = null)
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
            return Error.Failure("Directory.Enumerate", "Failed to enumerate files.");
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

    /// <summary>
    /// 単一のファイルをコピー元からコピー先へコピーします。
    /// </summary>
    /// <param name="sourceFile">コピー元のファイルパス。</param>
    /// <param name="destinationFile">コピー先のファイルパス。</param>
    /// <returns>成功した場合は <see cref="Success"/>、引数が無効・失敗した場合はエラーを返します。</returns>
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

    /// <summary>
    /// 指定したファイルパスの親ディレクトリが存在しない場合に作成します。
    /// </summary>
    /// <param name="filePath">ディレクトリを確保する対象のファイルパス。</param>
    public static void PrepareFileDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? filePath;
        Directory.CreateDirectory(directory);
    }

    /// <summary>
    /// 指定したルートディレクトリ配下のファイルを列挙します（既定で再帰的に走査します）。
    /// </summary>
    /// <param name="rootDirectory">列挙を開始するルートディレクトリのパス。</param>
    /// <param name="isRecursive"><see langword="true"/> の場合はサブディレクトリも再帰的に走査します。既定は <see langword="true"/> です。</param>
    /// <returns>見つかったファイルのパスを列挙する列挙可能オブジェクト。</returns>
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

    /// <summary>
    /// 指定したディレクトリ内で既存のファイル・フォルダと衝突しないユニークなパスを取得します。衝突する場合は名前にインデックスを付与します。
    /// </summary>
    /// <param name="directory">確認対象のディレクトリパス。</param>
    /// <param name="fileName">確認対象のファイル名またはフォルダ名。</param>
    /// <param name="isDirectory"><see langword="true"/> の場合はフォルダとして判定します。既定は <see langword="false"/> です。</param>
    /// <returns>衝突しないユニークなパス。</returns>
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

    /// <summary>
    /// 指定したディレクトリを削除します。存在しない場合や削除に失敗した場合は <see langword="false"/> を返します。
    /// </summary>
    /// <param name="path">削除するディレクトリのパス。</param>
    /// <param name="recursive"><see langword="true"/> の場合は配下の内容も含めて再帰的に削除します。既定は <see langword="true"/> です。</param>
    /// <returns>削除に成功した、または対象が存在しなかった場合は <see langword="true"/>、それ以外は <see langword="false"/>。</returns>
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
