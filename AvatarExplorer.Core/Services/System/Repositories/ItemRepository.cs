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

    /// <summary>
    /// アイテムが追加・更新・削除された際に発火します。引数は Item.Identifier です。
    /// </summary>
    public event Action<string>? OnItemUpdated;

    public void Load(string? path = null) => _db.Load(path);

    public IReadOnlyList<Item> GetAll() => _db.Items;
    public Item? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        _db.Remove(item.Id);
        OnItemUpdated?.Invoke(identifier);
    }

    public Item Create(ItemCreationContext context, bool save = true)
    {
        var item = new Item();
        item.UpdateMetadata(
            context.Title,
            context.Author,
            context.AuthorId,
            context.BoothId,
            new ItemCategory(context.ItemType, context.CustomCategory),
            context.ItemMemo
        );
        var now = DatetimeUtils.GetCurrentUnixTime();
        item.SetCreationDates(now, now);
        item.UpdateSupportedAvatars(context.SupportedAvatars);
        item.UpdateTags(context.Tags);

        _db.Add(item);

        if (save) Save();
        OnItemUpdated?.Invoke(item.Identifier);

        return item;
    }
    public bool Update(string identifier, ItemEditContext context, bool save = true)
    {
        var item = Get(identifier);
        if (item == null) return false;

        if (context.Title != null) item.UpdateTitle(context.Title);
        if (context.Author != null) item.UpdateAuthor(context.Author);
        if (context.AuthorId != null) item.UpdateAuthorId(context.AuthorId);
        if (context.BoothId != null) item.UpdateBoothId(context.BoothId.Value);
        if (context.ItemType != null) item.UpdateCategory(new ItemCategory(context.ItemType.Value, context.CustomCategory ?? item.Category.CustomCategory));
        if (context.ItemMemo != null) item.UpdateMemo(context.ItemMemo);

        if (context.SupportedAvatars != null) item.UpdateSupportedAvatars(context.SupportedAvatars);
        if (context.ImplementedAvatars != null) item.UpdateImplementedAvatars(context.ImplementedAvatars);
        if (context.Tags != null) item.UpdateTags(context.Tags);

        var now = DatetimeUtils.GetCurrentUnixTime();
        item.UpdateTimestamp(now);

        if (save) Save();
        OnItemUpdated?.Invoke(item.Identifier);

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
        OnItemUpdated?.Invoke(item.Identifier);

        return result;
    }

    public Dictionary<string, List<string>> CategorizeItems(IEnumerable<Item> items) // TODO: 順番どうにかしろ
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var item in items)
        {
            var key = item.Category.Type == ItemType.Custom && !string.IsNullOrWhiteSpace(item.Category.CustomCategory)
                ? $"custom:{item.Category.CustomCategory}"
                : $"type:{(int)item.Category.Type}";

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
        OnItemUpdated?.Invoke(item.Identifier);

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
        OnItemUpdated?.Invoke(item.Identifier);

        return Result.Success;
    }

    public void MergeCategory(ItemCategory sourceCategory, ItemCategory targetCategory)
    {
        var targetItems = GetAll()
            .Where(i => i.Category.Equals(sourceCategory))
            .ToList();

        targetItems.ForEach(i => i.UpdateCategory(targetCategory));
        targetItems.ForEach(i => OnItemUpdated?.Invoke(i.Identifier));

        // Save();
    }

    public void ValidateAndAutoFixItemType(bool avatarExist)
    {
        var items = GetAll();
        var unknownCategoryExists = items.Any(i => (int)i.Category.Type >= 11);
        if (avatarExist || unknownCategoryExists)
        {
            int offset = 0;
            if (avatarExist) offset = (int)items.Min(i => i.Category.Type) - (int)ItemType.Avatar;
            if (unknownCategoryExists) offset = (int)items.Max(i => i.Category.Type) - (int)ItemType.Custom;
            foreach (var item in items)
            {
                item.UpdateCategory(new(item.Category.Type - offset)); // TODO: バカ重いかも
                OnItemUpdated?.Invoke(item.Identifier);
            }
            // Save();
        }
    }

    public void Save() => _db.Save();
}
