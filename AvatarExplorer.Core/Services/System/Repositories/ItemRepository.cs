using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class ItemRepository : RepositoryBase<Item>
{
    public ItemRepository() : base(SystemPath.ItemDatabasePath) { }

    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            DatabaseMigrations.ItemVersion,
            (items, version) => DatabaseMigrations.ApplyItemMigration(items, version, AvatarExplorerApp.Instance.RuntimeSettings.Settings.DataRootDirectory));

        Db.Load();
        Db.MigrationVersion = DatabaseMigrations.ItemVersion;
        InvokeUpdated();
    }

    public override void Remove(string identifier) => Remove(identifier, removeFolder: false);

    public void Remove(string identifier, bool removeFolder)
    {
        var item = Get(identifier);
        if (item == null) return;

        if (removeFolder && Directory.Exists(item.ItemPath))
        {
            FileSystemService.DeleteDirectory(item.ItemPath);
        }

        Db.Remove(item.Id);
        Db.Save();
        InvokeUpdated();
    }

    public async Task<Item> Create(ItemCreationContext context)
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

        var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
        var downloaded = await context.FetchThumbnailAsync(destPath, overwrite: true);
        if (downloaded) item.UpdateThumbnailFileName(item.Id);

        Add(item);

        return item;
    }

    public async Task<bool> Update(string identifier, ItemEditContext context)
    {
        var item = Get(identifier);
        if (item == null) return false;

        if (context.Title != null) item.UpdateTitle(context.Title);
        if (context.Author != null) item.UpdateAuthor(context.Author);
        if (context.AuthorId != null) item.UpdateAuthorId(context.AuthorId);
        if (context.BoothId != null) item.UpdateBoothId(context.BoothId.Value);
        if (context.ItemType != null) item.UpdateCategory(new ItemCategory(context.ItemType.Value, context.CustomCategory ?? item.Category.CustomCategory));
        if (context.ItemMemo != null) item.UpdateMemo(context.ItemMemo);
        if (context.ItemPath != null) item.UpdateItemPath(context.ItemPath);

        if (context.SupportedAvatars != null) item.UpdateSupportedAvatars(context.SupportedAvatars);
        if (context.ImplementedAvatars != null) item.UpdateImplementedAvatars(context.ImplementedAvatars);
        if (context.Tags != null) item.UpdateTags(context.Tags);

        if (context.ThumbnailUrl != null)
        {
            var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
            var downloaded = await context.FetchThumbnailAsync(destPath, overwrite: true);
            if (downloaded) item.UpdateThumbnailFileName(item.Id);
        }

        item.UpdateTimestamp(DatetimeUtils.GetCurrentUnixTime());

        Save();
        InvokeUpdated();

        return true;
    }

    public async Task<ErrorOr<ExtractResult>> AddPaths(string identifier, IEnumerable<ItemPathEntry> paths, bool shouldLinkToOriginal, bool? removeOriginal = null)
    {
        static string GetSafePath(Item item, string dataRootDirectory)
        {
            var defaultFolderName = item.BoothId != -1 ? $"{item.BoothId} - {item.Title}" : item.Title;
            var folderName = ItemUtils.GetSafeTitle(defaultFolderName);
            if (string.IsNullOrEmpty(folderName))
            {
                if (item.BoothId != -1)
                    folderName = item.BoothId.ToString();
                else
                    folderName = item.Id;
            }

            return FileSystemService.GetUniquePath(dataRootDirectory, folderName, true);
        }

        var item = Get(identifier);
        if (item == null) return Error.NotFound(description: "Item not found.");

        var settings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;

        var defaultExtractPath = string.IsNullOrEmpty(item.ItemPath) ? GetSafePath(item, settings.DataRootDirectory) : item.ItemPath;
        var result = await FileSystemService.ExtractItemPaths(defaultExtractPath, paths, shouldLinkToOriginal, settings.MaxDegreeOfParallelism, removeOriginal ?? settings.RemoveOriginal);
        if (result.IsError) return Error.Failure(description: "Failed to extract item paths.");

        if (!string.IsNullOrEmpty(result.Value.ItemParentFolder)) item.UpdateItemPath(result.Value.ItemParentFolder);
        item.UpdateItemPaths(result.Value.FolderPaths);

        item.UpdateTimestamp(DatetimeUtils.GetCurrentUnixTime());

        Save();
        InvokeUpdated();

        return result;
    }

    /// <summary>
    /// アイテムのパスを削除します。ItemPathsに含まれている場合は追跡からも削除されます。
    /// </summary>
    /// <param name="identifier"></param>
    /// <param name="path"></param>
    /// <param name="deleteFolder"></param>
    /// <returns></returns>
    public async Task RemovePath(string identifier, string path, bool deleteFolder)
    {
        var item = Get(identifier);
        if (item == null) return;

        if (item.ItemPaths.Contains(path))
        {
            item.UpdateItemPaths(item.ItemPaths.Where(p => p != path));
        }

        if (deleteFolder && Directory.Exists(path))
        {
            FileSystemService.DeleteDirectory(path);
        }

        Save();
        InvokeUpdated();
    }

    public static IEnumerable<KeyValuePair<string, List<string>>> CategorizeItems(IEnumerable<Item> items)
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

        return result
            .OrderBy(kvp => kvp.Key.StartsWith("type:") ? 0 : 1)
            .ThenBy(kvp => kvp.Key.StartsWith("type:") ? int.Parse(kvp.Key[5..]) : 0)
            .ThenBy(kvp => kvp.Key);
    }

    public List<string> EnumerateItemFolders(string id)
    {
        var item = Get(id);
        if (item == null) return [];

        var folders = new List<string>();
        var root = item.ItemPath;

        if (Directory.Exists(root))
        {
            if (Directory.GetFiles(root).Length > 0)
                folders.Add(root);

            foreach (var dir in Directory.GetDirectories(root))
                folders.Add(dir);
        }

        foreach (var path in item.ItemPaths)
            folders.Add(path);

        return folders;
    }

    public List<ItemFile> EnumerateItemFiles(string id)
    {
        var item = Get(id);
        if (item == null) return [];

        var files = new List<ItemFile>();

        var root = item.ItemPath;
        foreach (var rootFile in FileSystemService.EnumerateFiles(root, isRecursive: false))
            files.Add(new(root, rootFile));

        if (Directory.Exists(root))
        {
            foreach (var rootFolder in Directory.GetDirectories(root))
                foreach (var rootFolderFile in FileSystemService.EnumerateFiles(rootFolder, isRecursive: true))
                    files.Add(new(rootFolder, rootFolderFile));
        }

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
        InvokeUpdated();

        return Result.Success;
    }

    public void RenameCustomCategory(string oldname, string newName)
    {
        var newCategory = new ItemCategory(newName);

        GetAll()
            .Where(i => i.Category.Type == ItemType.Custom && i.Category.CustomCategory == oldname)
            .ForEach(i => i.UpdateCategory(newCategory));

        Save();
        InvokeUpdated();
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
        var downloaded = await Downloader.Fetch(thumbnailUrl, destPath, overwrite: true);
        if (!downloaded) return Error.Failure(description: "Failed to download thumbnail.");

        item.UpdateThumbnailFileName(item.Id);

        // サムネイル取得は更新日時に影響を与えないようにする
        // item.UpdateTimestamp(DatetimeUtils.GetCurrentUnixTime());

        Save();
        InvokeUpdated();

        return Result.Success;
    }

    public void MergeCategory(ItemCategory sourceCategory, ItemCategory targetCategory)
    {
        var targetItems = GetAll()
            .Where(i => i.Category.Equals(sourceCategory))
            .ToList();

        targetItems.ForEach(i => i.UpdateCategory(targetCategory));

        Save();
        InvokeUpdated();
    }

    public void RenameTag(string oldTag, string newTag)
    {
        var targetItems = GetAll().Where(i => i.Tags.Contains(oldTag));
        if (!targetItems.Any()) return;

        targetItems
            .ForEach(i =>
            {
                var updatedTags = i.Tags.Select(t => t == oldTag ? newTag : t);
                i.UpdateTags(updatedTags);
            });

        Save();
        InvokeUpdated();
    }
    public void RemoveTag(string tag)
    {
        var targetItems = GetAll().Where(i => i.Tags.Contains(tag));
        if (!targetItems.Any()) return;

        targetItems
            .ForEach(i =>
            {
                var updatedTags = i.Tags.Where(t => t != tag);
                i.UpdateTags(updatedTags);
            });

        Save();
        InvokeUpdated();
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
                item.UpdateCategory(new(item.Category.Type - offset));
            }

            Save();
            InvokeUpdated();
        }
    }
}
