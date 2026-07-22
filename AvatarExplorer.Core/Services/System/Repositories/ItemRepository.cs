using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class ItemRepository
{
    private readonly DatabaseManager<Item> _db = new(SystemPath.ItemDatabasePath);
    public void Load(string? path = null) => _db.Load(path);

    public IReadOnlyList<Item> GetAll() => _db.Items;
    public Item? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        _db.Remove(item.Id);
    }

    public Item Create(ItemCreationContext context)
    {
        var item = new Item();
        item.UpdateMetadata(
            context.Title,
            context.Author,
            context.AuthorId,
            context.BoothId,
            context.ItemType,
            context.CustomCategory,
            context.ItemMemo
        );
        var now = DatetimeUtils.GetCurrentUnixTime();
        item.SetCreationDates(now, now);
        item.UpdateSupportedAvatars(context.SupportedAvatars);
        item.UpdateTags(context.Tags);

        _db.Add(item);
        return item;
    }
    public bool Update(string identifier, ItemEditContext context)
    {
        var item = Get(identifier);
        if (item == null) return false;

        if (context.Title != null) item.UpdateTitle(context.Title);
        if (context.Author != null) item.UpdateAuthor(context.Author);
        if (context.AuthorId != null) item.UpdateAuthorId(context.AuthorId);
        if (context.BoothId != null) item.UpdateBoothId(context.BoothId.Value);
        if (context.ItemType != null) item.UpdateItemType(context.ItemType.Value, context.CustomCategory ?? item.CustomCategory);
        if (context.ItemMemo != null) item.UpdateMemo(context.ItemMemo);

        if (context.SupportedAvatars != null) item.UpdateSupportedAvatars(context.SupportedAvatars);
        if (context.ImplementedAvatars != null) item.UpdateImplementedAvatars(context.ImplementedAvatars);
        if (context.Tags != null) item.UpdateTags(context.Tags);

        var now = DatetimeUtils.GetCurrentUnixTime();
        item.UpdateTimestamp(now);

        Save();

        return true;
    }

    public async Task<ErrorOr<ExtractResult>> AddPaths(string identifier, IEnumerable<string> paths, bool shouldLinkToOriginal)
    {
        static string GetSafePath(Item item, string dataRootDirectory)
        {
            string? folderName = ItemUtils.GetSafeTitle(item.Title);
            if (string.IsNullOrEmpty(folderName))
            {
                if (item.BoothId != -1)
                    folderName = item.BoothId.ToString();
                else
                    folderName = item.Id; // 最後の手段
            }

            return FileSystemService.GetUniquePath(dataRootDirectory, folderName, true);
        }
        
        var item = Get(identifier);
        if (item == null) return Error.NotFound(description: "Item not found.");

        var settings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;

        // Zipを展開する必要がある時はこれが使われる
        var defaultExtractPath = string.IsNullOrEmpty(item.ItemPath) ? GetSafePath(item, settings.DataRootDirectory) : item.ItemPath;
        var result = await FileSystemService.ExtractItemPaths(defaultExtractPath, paths, shouldLinkToOriginal, settings.MaxDegreeOfParallelism, settings.RemoveOriginal);
        
        if (!string.IsNullOrEmpty(result.Value.ItemParentFolder)) item.UpdateItemPath(result.Value.ItemParentFolder);
        item.UpdateItemPaths(result.Value.FolderPaths);
        
        return result;
    }

    public Dictionary<string, List<string>> CategorizeItems(IEnumerable<Item> items) // TODO: 順番どうにかしろ
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var item in items)
        {
            var key = item.Type == ItemType.Custom && !string.IsNullOrWhiteSpace(item.CustomCategory)
                ? $"custom:{item.CustomCategory}"
                : $"type:{(int)item.Type}";

            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(item.Id);
        }

        return result;
    }
    public List<ItemFile> EnumerateItemFiles(string id)
    {
        // Root
        var item = Get(id);
        if (item == null) return [];

        var files = new List<ItemFile>();

        // Root
        var root = item.ItemPath;
        foreach (var rootFile in FileSystemService.EnumerateFiles(root, isRecursive: false))
            files.Add(new(root, rootFile));

        foreach (var rootFolder in Directory.GetDirectories(root))
            foreach (var rootFolderFile in FileSystemService.EnumerateFiles(rootFolder, isRecursive: true))
                files.Add(new(rootFolder, rootFolderFile));

        // Other Folders
        foreach (var otherFolder in item.ItemPaths)
            foreach (var otherFile in FileSystemService.EnumerateFiles(otherFolder, isRecursive: true))
                files.Add(new(otherFolder, otherFile));

        return files;
    }

    public async Task<ErrorOr<Success>> UpdateThumbnail(string identifier, string imageFilePath)
    {
        var item = Get(identifier);
        if (item == null) return Error.NotFound(description: "Item not found.");

        var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
        var result = await FileSystemService.CopyFileAsync(imageFilePath, destPath);
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        item.UpdateThumbnailFileName(item.Id);
        item.UpdateTimestamp(DatetimeUtils.GetCurrentUnixTime());
        Save();

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> FetchThumbnailFromBooth(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return Error.NotFound(description: "Item not found.");
        if (item.BoothId == -1) return Error.Failure(description: "Item has no Booth ID.");

        var boothResult = await BoothService.Fetch(item.BoothId.ToString());
        if (boothResult.IsError) return Error.Failure(description: boothResult.Errors.ToErrorString());

        var thumbnailUrl = boothResult.Value.ThumbnailUrl;
        if (string.IsNullOrEmpty(thumbnailUrl)) return Error.Failure(description: "No thumbnail found on Booth.");

        var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
        var downloaded = await ImageDownloader.Fetch(thumbnailUrl, destPath, overwrite: true);
        if (!downloaded) return Error.Failure(description: "Failed to download thumbnail.");

        item.UpdateThumbnailFileName(item.Id);
        item.UpdateTimestamp(DatetimeUtils.GetCurrentUnixTime());
        Save();

        return Result.Success;
    }

    public void Save() => _db.Save();
}
