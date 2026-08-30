using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class ItemRepository : RepositoryBase<Item>
{
    /// <summary>アイテムデータのリポジトリを初期化します。</summary>
    public ItemRepository() : base(SystemPath.ItemDatabasePath) { }

    /// <summary>アイテムデータベースを読み込み、必要に応じてマイグレーションを適用します。</summary>
    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            DatabaseMigrations.ItemVersion,
            (items, version) => DatabaseMigrations.ApplyItemMigration(items, version, AvatarExplorerApp.Instance.RuntimeSettings.DataRootDirectory));

        Db.Load();
        Db.MigrationVersion = DatabaseMigrations.ItemVersion;
        InvokeUpdated();
    }

    /// <summary>指定したIdentifierのアイテムを削除します。関連するフォルダは削除されません。</summary>
    /// <param name="identifier">削除するアイテムのIdentifier。</param>
    public override void Remove(string identifier) => Remove(identifier, removeFolder: false);

    /// <summary>指定したIdentifierのアイテムを削除します。removeFolderがtrueの場合はアイテムのルートフォルダも削除します。</summary>
    /// <param name="identifier">削除するアイテムのIdentifier。</param>
    /// <param name="removeFolder">アイテムのルートフォルダも削除するかどうか。</param>
    public void Remove(string identifier, bool removeFolder)
    {
        var item = Get(identifier);
        if (item == null) return;

        var rootPath = item.GetItemPath();
        if (removeFolder && Directory.Exists(rootPath))
        {
            FileSystemService.DeleteDirectory(rootPath);
        }

        Db.Remove(item.Id);
        Db.Save();
        InvokeUpdated();
    }

    /// <summary>指定した作成コンテキストから新しいアイテムを作成し、データベースに保存します。BoothIdが指定されている場合は、バックグラウンドでバリエーションハッシュのシードも行われます（アイテム作成自体は失敗しません）。</summary>
    /// <param name="context">アイテムの作成に使用するコンテキスト。</param>
    /// <returns>作成されたアイテム。</returns>
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
        item.UpdateIsHidden(context.IsHidden);
        item.UpdateExcludeFromCommonAvatarCheck(context.ExcludeFromCommonAvatarCheck);

        var destPath = Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id);
        var downloaded = await context.FetchThumbnailAsync(destPath, overwrite: true);
        if (downloaded) item.UpdateThumbnailFileName(item.Id);

        Add(item);

        // BoothIdがある場合は、Createの成否に影響を与えずに非同期でVariationHashをシードする
        if (item.BoothId != -1)
        {
            _ = SeedVariationHashAsync(item.BoothId.ToString());
        }

        return item;
    }

    /// <summary>
    /// アイテム作成時にバックグラウンドでVariationHashをシードします。
    /// ネットワーク障害が発生してもアイテム作成自体には影響しないよう、エラーはErrorManagerへ通報します。
    /// </summary>
    private static async Task SeedVariationHashAsync(string itemId)
    {
        try
        {
            await AvatarExplorerApp.Instance.VariationHashRepository.EnsureVariationHash(itemId);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError(
                $"Failed to seed variation hash for item '{itemId}'.",
                ex
            );
        }
    }

    /// <summary>指定した編集コンテキストの内容でアイテムを更新します。nullでないプロパティのみが上書きされます。</summary>
    /// <param name="identifier">更新対象のアイテムのIdentifier。</param>
    /// <param name="context">適用する更新内容。</param>
    /// <returns>更新に成功した場合はtrue、アイテムが見つからない場合はfalse。</returns>
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
        if (context.IsHidden != null) item.UpdateIsHidden(context.IsHidden.Value);
        if (context.ExcludeFromCommonAvatarCheck != null) item.UpdateExcludeFromCommonAvatarCheck(context.ExcludeFromCommonAvatarCheck.Value);

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

    /// <summary>アイテムにファイル・フォルダのパスを追加（展開または元フォルダへのリンク）し、データベースを更新します。</summary>
    /// <param name="identifier">対象のアイテムのIdentifier。</param>
    /// <param name="paths">追加するパス情報の列挙可能なコレクション。</param>
    /// <param name="shouldLinkToOriginal">パスがフォルダの場合に、コピーせず元フォルダへリンクするかどうか。</param>
    /// <param name="removeOriginal">アーカイブの場合に展開後に元ファイルを削除するかどうか。nullの場合はRuntimeSettingsの値が使用されます。</param>
    /// <returns>展開結果（展開先フォルダ等）を含むErrorOr。アイテムが見つからない場合はNotFoundエラー。</returns>
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

        var settings = AvatarExplorerApp.Instance.RuntimeSettings;

        var currentRootPath = item.GetItemPath();
        var defaultExtractPath = string.IsNullOrEmpty(currentRootPath) ? GetSafePath(item, settings.DataRootDirectory) : currentRootPath;
        var result = await FileSystemService.ExtractItemPaths(defaultExtractPath, paths, shouldLinkToOriginal, settings.MaxDegreeOfParallelism, removeOriginal ?? settings.RemoveOriginal);
        if (result.IsError) return Error.Failure(description: "Failed to extract item paths.");

        if (!string.IsNullOrEmpty(result.Value.ItemParentFolder)) item.UpdateItemPath(ItemUtils.GetRelativePath(result.Value.ItemParentFolder));
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

    /// <summary>アイテム一覧をカテゴリIdentifier別にグループ化します。組み込みカテゴリ（type:）が先に、カスタムカテゴリが後に並びます。</summary>
    /// <param name="items">グループ化するアイテムの列挙可能なコレクション。</param>
    /// <returns>カテゴリIdentifierと、そのカテゴリに属するアイテムIDのリストのペアの列挙可能なコレクション。</returns>
    public static IEnumerable<KeyValuePair<string, List<string>>> CategorizeItems(IEnumerable<Item> items)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var item in items)
        {
            var key = item.Category.Identifier;

            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(item.Id);
        }

        return result
            .OrderBy(kvp => kvp.Key.StartsWith(ItemCategory.TypeCategoryPrefix) ? 0 : 1)
            .ThenBy(kvp => kvp.Key.StartsWith(ItemCategory.TypeCategoryPrefix) ? int.Parse(kvp.Key[5..]) : 0)
            .ThenBy(kvp => kvp.Key);
    }

    /// <summary>指定したアイテムのルートパスおよび追加パスに含まれるフォルダ一覧を取得します。</summary>
    /// <param name="id">対象のアイテムのIdentifier（またはID）。</param>
    /// <returns>フォルダパスのリスト。アイテムが見つからない場合は空のリスト。</returns>
    public List<string> EnumerateItemFolders(string id)
    {
        var item = Get(id);
        if (item == null) return [];

        var folders = new List<string>();
        var root = item.GetItemPath();

        if (Directory.Exists(root))
        {
            if (Directory.GetFiles(root).Length > 0)
                folders.Add(root);

            folders.AddRange(Directory.GetDirectories(root));
        }

        folders.AddRange(item.ItemPaths);

        return folders;
    }

    /// <summary>指定したアイテムに含まれる全ファイルを再帰的に列挙します。</summary>
    /// <param name="id">対象のアイテムのIdentifier（またはID）。</param>
    /// <returns>アイテムファイルのリスト。アイテムが見つからない場合は空のリスト。</returns>
    public List<ItemFile> EnumerateItemFiles(string id)
    {
        var item = Get(id);
        if (item == null) return [];

        var files = new List<ItemFile>();

        var root = item.GetItemPath();
        if (Directory.Exists(root))
        {
            foreach (var rootFile in FileSystemService.EnumerateFiles(root, isRecursive: false))
                files.Add(new(root, rootFile));

            foreach (var rootFolder in Directory.GetDirectories(root))
            {
                foreach (var rootFolderFile in FileSystemService.EnumerateFiles(rootFolder, isRecursive: true))
                {
                    files.Add(new(rootFolder, rootFolderFile));
                }
            }
        }

        foreach (var otherFolder in item.ItemPaths)
        {
            foreach (var otherFile in FileSystemService.EnumerateFiles(otherFolder, isRecursive: true))
            {
                files.Add(new(otherFolder, otherFile));
            }
        }

        return files;
    }

    /// <summary>指定したアイテム内のファイルを検索文字列で絞り込みます。フィールド指定トークンは無視されます。</summary>
    /// <param name="id">対象のアイテムのIdentifier（またはID）。</param>
    /// <param name="searchString">ファイル名に対して部分一致（大文字小文字区別なし）で検索する文字列。</param>
    /// <returns>条件に一致したアイテムファイルの配列。アイテムが見つからない場合は空の配列。</returns>
    public ItemFile[] SearchItemFiles(string id, string searchString)
    {
        var item = Get(id);
        if (item == null) return [];

        var query = SearchQueryParser.Parse(searchString);
        var files = EnumerateItemFiles(id);

        return files
            .Where(f => query.Tokens.All(t => IsMatch(f.FileName, t)))
            .ToArray();

        static bool IsMatch(string fileName, SearchQueryToken token)
        {
            if (token.Field != null) return false; // フィールド指定がある場合は無視する
            var value = token.Value;
            if (token.IsNegation) return !fileName.Contains(value, StringComparison.OrdinalIgnoreCase);
            else return fileName.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>指定した画像ファイルからアイテムのサムネイルを更新します。</summary>
    /// <param name="identifier">対象のアイテムのIdentifier。</param>
    /// <param name="imageFilePath">サムネイルとして設定する画像ファイルのパス。</param>
    /// <returns>成功した場合はSuccess、アイテムが見つからない場合はNotFoundエラー。</returns>
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

    /// <summary>カスタムカテゴリ名を一括で変更します。該当する全アイテムのカテゴリが新しい名前に更新されます。</summary>
    /// <param name="oldname">変更前のカスタムカテゴリ名。</param>
    /// <param name="newName">変更後のカスタムカテゴリ名。</param>
    public void RenameCustomCategory(string oldname, string newName)
    {
        var newCategory = new ItemCategory(newName);

        GetAll()
            .Where(i => i.Category.Type == ItemType.Custom && i.Category.CustomCategory == oldname)
            .ForEach(i => i.UpdateCategory(newCategory));

        Save();
        InvokeUpdated();
    }

    /// <summary>Boothから商品のサムネイル画像を取得し、アイテムのサムネイルを更新します。</summary>
    /// <param name="identifier">対象のアイテムのIdentifier。</param>
    /// <returns>成功した場合はSuccess、アイテムが見つからない、またはBoothIdが未設定・サムネイル取得失敗の場合はエラー。</returns>
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

        Save();
        InvokeUpdated();

        return Result.Success;
    }

    /// <summary>指定したソースカテゴリに属する全アイテムをターゲットカテゴリに統合（移動）します。</summary>
    /// <param name="sourceCategory">統合元のカテゴリ。</param>
    /// <param name="targetCategory">統合先のカテゴリ。</param>
    public void MergeCategory(ItemCategory sourceCategory, ItemCategory targetCategory)
    {
        var targetItems = GetAll()
            .Where(i => i.Category.Equals(sourceCategory))
            .ToList();

        targetItems.ForEach(i => i.UpdateCategory(targetCategory));

        Save();
        InvokeUpdated();
    }

    /// <summary>タグ名を一括で変更します。該当する全アイテムのタグが新しい名前に更新されます。</summary>
    /// <param name="oldTag">変更前のタグ名。</param>
    /// <param name="newTag">変更後のタグ名。</param>
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
    /// <summary>指定したタグを全アイテムから削除します。</summary>
    /// <param name="tag">削除するタグ名。</param>
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

    /// <summary>データベース内のアイテムのカテゴリ（ItemType）を検証し、必要に応じて自動修正します。avatarExistがtrueの場合、または未定義のカテゴリ（Type値が11以上）が存在する場合に実行されます。</summary>
    /// <param name="avatarExist">ユーザーがアバターを追加したことがあるかどうか。trueの場合はカテゴリのずれを補正します。</param>
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
                item.UpdateCategory(new(item.Category.Type - offset, item.Category.CustomCategory));
            }

            Save();
            InvokeUpdated();
        }
    }
}
