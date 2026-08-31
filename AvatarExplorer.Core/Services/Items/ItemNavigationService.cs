using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

/// <summary>
/// アイテム・フォルダ・ファイルの階層をIdentifierベースで選択（ナビゲーション）するためのサービスです。
/// アバター→カテゴリ→アイテム→フォルダ→拡張子→ファイルの順に選択状態を管理します。
/// </summary>
public class ItemNavigationService
{
    /// <summary>アバターを表すIdentifierのプレフィックス（"avatar"）です。</summary>
    public const string AvatarPrefix = "avatar";
    /// <summary>作者を表すIdentifierのプレフィックス（"author"）です。</summary>
    public const string AuthorPrefix = "author";
    /// <summary>組み込みのタイプカテゴリを表すIdentifierのプレフィックス（"type"）です。</summary>
    public const string TypePrefix = "type";
    /// <summary>カスタムカテゴリを表すIdentifierのプレフィックス（"custom"）です。</summary>
    public const string CustomPrefix = "custom";
    /// <summary>アイテムを表すIdentifierのプレフィックス（"item"）です。</summary>
    public const string ItemPrefix = "item";
    /// <summary>フォルダを表すIdentifierのプレフィックス（"folder"）です。</summary>
    public const string FolderPrefix = "folder";
    /// <summary>ファイルの拡張子を表すIdentifierのプレフィックス（"extension"）です。</summary>
    public const string ExtensionPrefix = "extension";
    /// <summary>ファイルを表すIdentifierのプレフィックス（"file"）です。</summary>
    public const string FilePrefix = "file";

    private readonly ItemGroupService _items;
    private readonly SelectionState _state = new();
    private readonly Dictionary<string, string> _pathCache = [];

    private readonly Dictionary<string, Func<string, IIdentifiable[]>> _handlers;

    /// <summary>
    /// ファイル（<c>file:</c> プレフィックスのIdentifier）が選択されたときに発火するイベントです。
    /// 引数には選択されたファイルのフルパスが渡されます。
    /// </summary>
    public event Action<string>? FileOpenRequested = null;

    /// <summary>
    /// プレフィックスと値から <c>"プレフィックス:値"</c> 形式のIdentifierを生成します。
    /// </summary>
    /// <param name="prefix">プレフィックス（<see cref="AvatarPrefix"/> 等）。</param>
    /// <param name="value">値となる文字列。</param>
    /// <returns><c>"プレフィックス:値"</c> 形式のIdentifier。</returns>
    public static string GetPrefix(string prefix, string value) => $"{prefix}:{value}";

    internal ItemNavigationService(ItemGroupService itemGroupService)
    {
        _handlers = new()
        {
            { AvatarPrefix, HandleRoot },
            { AuthorPrefix, HandleRoot },
            { TypePrefix, HandleCategory },
            { CustomPrefix, HandleCategory },
            { ItemPrefix, HandleItem },
            { FolderPrefix, HandleFolder },
            { ExtensionPrefix, HandleExtension }
        };

        _items = itemGroupService;
    }

    /// <summary>
    /// 指定したIdentifierを選択（ナビゲーション状態にプッシュ）します。
    /// ファイル（<c>file:</c>）を選択した場合は <see cref="FileOpenRequested"/> イベントが発火し、<c>null</c> を返します。
    /// </summary>
    /// <param name="state">選択する対象のIdentifier（<c>avatar:</c>, <c>type:</c>, <c>item:</c>, <c>folder:</c>, <c>extension:</c>, <c>file:</c> 等）。</param>
    /// <returns>
    /// ファイル選択以外の場合は新しく作成された選択ノードのID（<see cref="Guid"/>）。
    /// ファイル選択の場合、または解析に失敗した場合は <c>null</c>。
    /// </returns>
    public Guid? Select(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return null;

        if (prefix == FilePrefix)
        {
            HandleFile(value);
            return null;
        }

        return _state.Push(state);
    }
    /// <summary>
    /// 選択状態を一つ前に戻します（最後にプッシュしたノードをポップします）。
    /// </summary>
    /// <returns>ポップされた選択ノード。これ以上戻せない場合は <c>null</c>。</returns>
    public SelectionNode? Undo() => _state.Pop();
    /// <summary>
    /// すべての選択状態を解除し、ルート（初期状態）に戻します。パスのキャッシュもクリアされます。
    /// </summary>
    public void Clear()
    {
        _state.Clear();
        _pathCache.Clear();
    }
    /// <summary>
    /// 指定したIdentifierの状態まで選択履歴を一気に戻します。
    /// </summary>
    /// <param name="targetState">戻り先のIdentifier。履歴に存在しない場合は何もしません。</param>
    public void PopToState(string targetState)
    {
        var nodes = _state.GetCurrentSelectionNodes().ToList();
        if (nodes.All(n => n.Value != targetState)) return;

        while (_state.Current != null && _state.Current.Value != targetState)
        {
            _state.Pop();
        }
    }

    /// <summary>現在の選択状態（最後にプッシュされたノード）を取得します。何も選択されていない場合は <c>null</c>。</summary>
    public SelectionNode? CurrentState => _state.Current;
    /// <summary>現在の選択履歴（ルートから現在までの全ノード）を取得します。</summary>
    /// <returns>ルートから現在の順に並んだ選択ノードの列挙可能なコレクション。</returns>
    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _state.GetCurrentSelectionNodes();

    /// <summary>現在選択されているアバター（<c>avatar:</c>）のID部分を取得します。</summary>
    /// <returns>アバターID。アバターが選択されていない場合は <c>null</c>。</returns>
    public string? GetCurrentAvatarId()
    {
        var avatarNode = _state.LastOrDefault(AvatarPrefix);
        if (avatarNode == null) return null;

        if (!TryParseState(avatarNode.Value, out var _, out var avatarId)) return null;

        return avatarId;
    }
    /// <summary>現在選択されているアイテム（<c>item:</c>）のID部分を取得します。</summary>
    /// <returns>アイテムID。アイテムが選択されていない場合は <c>null</c>。</returns>
    public string? GetCurrentItemId() => _state.FirstOrDefault(ItemPrefix)?.Value;

    /// <summary>
    /// 指定したIdentifier（フォルダまたはファイル）のハッシュから元のフルパスを解決します。
    /// 選択操作によりパスキャッシュが構築された後にのみ有効です。
    /// </summary>
    /// <param name="state">フォルダまたはファイルのIdentifier。</param>
    /// <returns>解決されたフルパス。見つからない場合は <c>null</c>。</returns>
    public string? ResolvePath(string state) => TryParseState(state, out _, out var hash) ? ResolvePathInternal(hash) : null;
    private string? ResolvePathInternal(string hash) => _pathCache.TryGetValue(hash, out var path) ? path : null;

    /// <summary>
    /// 現在の選択状態に応じて表示可能なオブジェクト一覧を取得します。
    /// 初期状態では全アイテム（非表示除く）、それ以降はカテゴリ別フォルダやアイテム内のフォルダ・ファイル等が返されます。
    /// </summary>
    /// <returns>現在のビューに対応する <see cref="IIdentifiable"/> オブジェクトの配列。</returns>
    public IIdentifiable[] GetCurrentSelectionView()
    {
        var state = _state.Current?.Value;
        if (state == null)
        {
            return _items.ItemRepository.GetAll()
                .Where(i => !i.IsHidden)
                .ToArray<IIdentifiable>();
        }

        if (!TryParseState(state, out var key, out _)) return [];
        return _handlers.TryGetValue(key, out var func) ? func(state) : [];
    }
    private IIdentifiable[] HandleRoot(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        List<Item>? items = prefix switch
        {
            AvatarPrefix => _items.GetItemsFromAvatar(value),
            AuthorPrefix => _items.GetItemsFromAuthor(value),
            _ => null
        };

        if (items == null) return [];

        var categolized = ItemRepository.CategorizeItems(items)
        .Select(i =>
        {
            var category = ItemCategory.FromIdentifier(i.Key);
            return new Folder(i.Key)
            {
                Title = category.ToString(),
                TitleLocalizable = category.IsLocalizable,
                ItemCount = i.Value.Count
            };
        }).ToList<IIdentifiable>();

        // アバター選択時にアバターカテゴリを非表示にする設定が有効な場合、アバターカテゴリを除外する
        if (prefix == AvatarPrefix && AvatarExplorerApp.Instance.RuntimeSettings.HideAvatarCategoryWhenAvatarSelected)
        {
            categolized.RemoveAll(f => f.Identifier == ItemCategory.Avatar.Identifier);
        }

        // Hidden
        if (items.Any(i => i.IsHidden))
        {
            categolized.Add(new Folder(ItemCategory.Hidden.Identifier)
            {
                Title = ItemCategory.Hidden.ToString(),
                TitleLocalizable = ItemCategory.Hidden.IsLocalizable,
                ItemCount = items.Count(i => i.IsHidden)
            });
        }

        return categolized.ToArray();
    }
    private IIdentifiable[] HandleCategory(string state)
    {
        var root = _state.Root?.Value;
        if (root == null) return [];

        IEnumerable<Item> items;
        if (TryParseState(root, out var rootPrefix, out var rootValue) && rootPrefix == AvatarPrefix)
            items = _items.GetItemsFromAvatar(rootValue);
        else if (TryParseState(root, out rootPrefix, out rootValue) && rootPrefix == AuthorPrefix)
            items = _items.GetItemsFromAuthor(rootValue);
        else
            items = _items.ItemRepository.GetAll();

        if (ItemCategory.FromIdentifier(state).Type == ItemType.All)
            return items.Where(i => !i.IsHidden).ToArray();

        if (ItemCategory.FromIdentifier(state).Type == ItemType.Hidden)
            return items.Where(i => i.IsHidden).ToArray();

        return items.Where(i => i.Category.Identifier == state && !i.IsHidden).ToArray();
    }
    private IIdentifiable[] HandleItem(string state)
    {
        var itemFiles = PopulatePathCache(state);
        var allFolders = _items.ItemRepository.EnumerateItemFolders(GetCurrentItemId() ?? string.Empty);

        foreach (var folder in allFolders)
            _pathCache[PathUtils.ComputeHash(folder)] = folder;

        return allFolders
            .NaturalSort(i => Path.GetFileName(i))
            .Select(folder =>
            {
                var hash = PathUtils.ComputeHash(folder);
                return new Folder(GetPrefix(FolderPrefix, hash), folder)
                {
                    Title = Path.GetFileName(folder),
                    TitleLocalizable = false,
                    ItemCount = itemFiles.Count(f => f.ParentFolderPath == folder)
                };
            })
            .ToArray<IIdentifiable>();
    }
    private IIdentifiable[] HandleFolder(string state)
    {
        if (!TryParseState(state, out _, out var hash)) return [];

        var itemId = GetCurrentItemId();
        if (itemId == null) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePathInternal(hash);
        if (selectedFolderPath == null) return [];

        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);
        var categolized = FileCategorizer.Categorize(files);

        return categolized.Select(i =>
        {
            var localizationKey = i.Key.GetLocalizationKey();
            return new Folder(GetPrefix(ExtensionPrefix, ((int)i.Key).ToString()))
            {
                Title = localizationKey ?? i.Key.ToString(),
                TitleLocalizable = localizationKey != null,
                ItemCount = i.Value.Count
            };
        }).ToArray<IIdentifiable>();
    }
    private IIdentifiable[] HandleExtension(string state)
    {
        if (!TryParseState(state, out _, out var categoryRaw)) return [];

        var categoryIndex = ValueParser.Int(categoryRaw);
        if (!Enum.IsDefined(typeof(ItemFileCategoryType), categoryIndex)) return [];

        var itemId = GetCurrentItemId();
        var folderState = _state.FirstOrDefault(FolderPrefix)?.Value;
        if (itemId == null || folderState == null) return [];
        if (!TryParseState(folderState, out _, out var folderHash)) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePathInternal(folderHash);
        if (selectedFolderPath == null) return [];

        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);
        var categolized = FileCategorizer.Categorize(files);

        return categolized.TryGetValue((ItemFileCategoryType)categoryIndex, out var categorizedFiles)
            ? categorizedFiles.NaturalSort(i => i.FileName).ToArray()
            : [];
    }
    private void HandleFile(string hash)
    {
        var itemid = GetCurrentItemId();
        if (itemid == null) return;

        PopulatePathCache(itemid);

        var file = ResolvePathInternal(hash);
        if (file == null) return;

        FileOpenRequested?.Invoke(file);
    }

    /// <summary>
    /// 現在選択されているアイテム内のファイルを検索します。
    /// フォルダが選択されている場合はそのフォルダ内で、フォルダが選択されていない場合はアイテム全体で検索を行います。
    /// アイテムが選択されていない場合は空の配列を返します。
    /// </summary>
    /// <param name="query">検索クエリ（ファイル名の一部や拡張子など）。</param>
    /// <returns>条件に一致した <see cref="ItemFile"/> を <see cref="IIdentifiable"/> として格納した配列。</returns>
    public IIdentifiable[] SearchFilesForCurrentItem(string query)
    {
        var itemId = GetCurrentItemId();
        if (itemId == null) return [];

        // フォルダーが選択されていたら、そのフォルダー内で検索をかける
        var folderState = _state.FirstOrDefault(FolderPrefix)?.Value;
        var folderPath = folderState != null && TryParseState(folderState, out _, out var folderHash)
            ? ResolvePathInternal(folderHash)
            : null;

        var itemFiles = PopulatePathCache(itemId);
        var searchResults = _items.ItemRepository.SearchItemFiles(itemId, query);

        return searchResults
            .Where(i => folderPath == null || i.ParentFolderPath == folderPath)
            .NaturalSort(i => i.FileName)
            .Select(f => itemFiles.FirstOrDefault(i => i.FilePath == f.FilePath))
            .Where(f => f != null)
            .Cast<IIdentifiable>()
            .ToArray();
    }

    private List<ItemFile> PopulatePathCache(string itemId)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemId);

        foreach (var file in itemFiles.Select(i => i.FilePath))
            _pathCache[PathUtils.ComputeHash(file)] = file;

        return itemFiles;
    }

    /// <summary>
    /// Identifier文字列をプレフィックス（<c>:</c> より前）と値（<c>:</c> より後）に分割します。
    /// </summary>
    /// <param name="rawState">解析対象のIdentifier文字列。</param>
    /// <param name="key">分割されたプレフィックス（キー）。失敗時は空文字列。</param>
    /// <param name="value">分割された値。失敗時は空文字列。</param>
    /// <returns>分割に成功した場合は <c>true</c>、<c>:</c> が含まれていない場合は <c>false</c>。</returns>
    public static bool TryParseState(string rawState, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;
        var delimiterIndex = rawState.IndexOf(':');
        if (delimiterIndex < 0) return false;
        key = rawState[..delimiterIndex];
        value = rawState[(delimiterIndex + 1)..];
        return true;
    }
}
