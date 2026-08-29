using System.Collections.Concurrent;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

/// <summary>ナビゲーション用のサイドパネルフィルタとして取得する対象の種別。</summary>
public enum QueryType
{
    /// <summary>アバター一覧（共通素体・仮アバターを含む）。</summary>
    Avatar,
    /// <summary>作者一覧。</summary>
    Author,
    /// <summary>カテゴリ一覧。</summary>
    Category
}

/// <summary>
/// アイテム・共通素体・仮アバターにまたがる横断的な操作（検索・削除・インポート/エクスポート・フィルタ取得など）を提供するサービス。
/// </summary>
public class ItemGroupService
{
    private readonly TempAvatarRepository _tempAvatars;
    private readonly RuntimeSettingsRepository _runtimesettings;

    private readonly ConcurrentDictionary<string, ItemSearchIndex> _itemSearchIndices = new();
    private readonly ConcurrentDictionary<string, CommonAvatarSearchIndex> _commonAvatarSearchIndices = new();
    private readonly ConcurrentDictionary<string, TempAvatarSearchIndex> _tempAvatarSearchIndices = new();
    private bool _indicesBuilt;
    private readonly Lock _indicesLock = new();

    internal ItemRepository ItemRepository { get; }
    internal CommonAvatarRepository CommonAvatarRepository { get; }

    /// <summary>
    /// <see cref="ItemGroupService"/> を初期化します。各リポジトリを保持し、データベース更新時に検索インデックスを再構築するよう購読します。
    /// </summary>
    /// <param name="items">アイテムリポジトリ。</param>
    /// <param name="commonAvatars">共通素体リポジトリ。</param>
    /// <param name="tempAvatars">仮アバターリポジトリ。</param>
    /// <param name="settings">設定リポジトリ。</param>
    public ItemGroupService(ItemRepository items, CommonAvatarRepository commonAvatars, TempAvatarRepository tempAvatars, RuntimeSettingsRepository settings)
    {
        ItemRepository = items;
        CommonAvatarRepository = commonAvatars;
        _tempAvatars = tempAvatars;
        _runtimesettings = settings;

        ItemRepository.OnUpdated += OnDatabaseUpdated;
        CommonAvatarRepository.OnUpdated += OnDatabaseUpdated;
        _tempAvatars.OnUpdated += OnDatabaseUpdated;
    }

    /// <summary>指定した種別 (<see cref="QueryType"/>) に応じたサイドパネル用フィルタ一覧（アバター・作者・カテゴリ）を取得します。</summary>
    /// <param name="type">取得するフィルタの種別。</param>
    /// <returns>フィルタを表す識別可能オブジェクトのリスト。</returns>
    public List<IIdentifiable> GetQueryFilters(QueryType type)
    {
        return type switch
        {
            QueryType.Avatar => GetAvatars(includeTempAvatar: true),
            QueryType.Author => GetAuthors(),
            QueryType.Category => GetCategoryFolders(includeAllCategory: true, includeHiddenCategory: true),
            _ => []
        };
    }
    /// <summary>
    /// アバター一覧を取得します。<paramref name="includeCommonAvatar"/> で共通素体、<paramref name="includeTempAvatar"/> で仮アバターを含めるか指定できます。
    /// <paramref name="rawIdentifier"/> が false の場合は "avatar:" プレフィックス付きの識別子で返されます。
    /// </summary>
    /// <param name="includeCommonAvatar">共通素体グループを含めるかどうか。</param>
    /// <param name="includeTempAvatar">仮アバターを含めるかどうか。</param>
    /// <param name="rawIdentifier"><see langword="true"/> の場合は "avatar:" プレフィックスを付けずに返します。</param>
    /// <returns>アバターを表す <see cref="IIdentifiable"/> のリスト。</returns>
    public List<IIdentifiable> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false, bool rawIdentifier = false)
    {
        var avatars = new List<IIdentifiable>();

        if (includeCommonAvatar) avatars.AddRange(CommonAvatarRepository.GetAll());
        avatars.AddRange(ItemRepository.GetAll().Where(i => i.Category.Type == ItemType.Avatar));
        if (includeTempAvatar) avatars.AddRange(_tempAvatars.GetAll());

        return avatars.ConvertAll<IIdentifiable>(i => new Avatar(i, rawIdentifier));
    }
    /// <summary>全アイテムを作者名でグループ化し、作者一覧を取得します。各作者のアイテム数も含まれます。</summary>
    /// <returns>作者名とアイテム数を持つ <see cref="IIdentifiable"/> のリスト。</returns>
    public List<IIdentifiable> GetAuthors()
    {
        return ItemRepository.GetAll()
            .GroupBy(i => i.Author)
            .Select(i => new Author()
            {
                Name = i.Key,
                ItemCount = i.Count()
            })
            .OrderBy(i => i.Name)
            .ToList<IIdentifiable>();
    }
    /// <summary>
    /// カテゴリ別のフォルダ一覧を取得します。組み込みカテゴリ、カスタムカテゴリ、およびオプションで「すべて」「非表示」カテゴリを含めることができます。
    /// </summary>
    /// <param name="includeEmptyCategory">アイテムが存在しないカテゴリも含めるかどうか。</param>
    /// <param name="includeAllCategory">「すべて」カテゴリを含めるかどうか。</param>
    /// <param name="includeHiddenCategory">非表示アイテム用のカテゴリを含めるかどうか。</param>
    /// <returns>カテゴリを表すフォルダのリスト。</returns>
    public List<IIdentifiable> GetCategoryFolders(bool includeEmptyCategory = false, bool includeAllCategory = false, bool includeHiddenCategory = false)
    {
        var categories = new List<Folder>();

        var items = ItemRepository.GetAll();

        if (includeAllCategory)
        {
            categories.Add(new Folder(new ItemCategory(ItemType.All).Identifier)
            {
                Title = ItemType.All.GetLocalizationKey() ?? string.Empty,
                TitleLocalizable = true,
                ItemCount = items.Count
            });
        }

        var itemsByType = items
            .GroupBy(i => i.Category.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var itemsByCustomCategory = items
            .Where(i => i.Category.Type == ItemType.Custom && !string.IsNullOrEmpty(i.Category.CustomCategory))
            .GroupBy(i => i.Category.CustomCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        var existCategories = items.Select(i => i.Category.Type).Distinct();
        var existCustomCategories = items.Where(i => i.Category.Type == ItemType.Custom).Select(i => i.Category.CustomCategory).Distinct();

        categories.AddRange(
            Enum.GetValues<ItemType>()
                .Where(i => i.IsSelectable() && (includeEmptyCategory || existCategories.Contains(i)))
                .Select(i =>
                {
                    return new Folder(new ItemCategory(i).Identifier)
                    {
                        Title = i.GetLocalizationKey() ?? string.Empty,
                        TitleLocalizable = true,
                        ItemCount = itemsByType.TryGetValue(i, out int count) ? count : 0
                    };
                })
        );

        categories.AddRange(existCustomCategories.Select(i =>
        {
            return new Folder(new ItemCategory(i).Identifier)
            {
                Title = i,
                TitleLocalizable = false,
                ItemCount = itemsByCustomCategory[i]
            };
        }));

        if (includeHiddenCategory)
        {
            var hiddenCount = items.Count(i => i.IsHidden);
            if (hiddenCount > 0)
            {
                var hiddenCategory = new ItemCategory(ItemType.Hidden);
                categories.Add(new Folder(hiddenCategory.Identifier)
                {
                    Title = hiddenCategory.ToString(),
                    TitleLocalizable = hiddenCategory.IsLocalizable,
                    ItemCount = hiddenCount
                });
            }
        }

        return categories.ToList<IIdentifiable>();
    }
    /// <summary>
    /// 指定したアバター ID（通常アバター・共通素体・仮アバターのいずれも可）に対応するアイテム一覧を取得します。
    /// 共通素体グループの場合は、グループ内のアバターのいずれかに対応するアイテムも含まれます。
    /// </summary>
    /// <param name="id">対象のアバター識別子。</param>
    /// <returns>対応するアイテムのリスト。</returns>
    public List<Item> GetItemsFromAvatar(string id)
    {
        var items = ItemRepository.GetAll();
        var commonAvatars = CommonAvatarRepository.GetAll();
        var treatEmptyAsNone = _runtimesettings.Settings.TreatEmptySupportedAvatarAsNone;

        return items
            .Where(i =>
            {
                var result = AvatarStatusResolver.Resolve(i, id, commonAvatars, treatEmptyAsNone);
                return result.IsSupportedOrCommon;
            })
            .ToList();
    }
    /// <summary>指定した作者が作成したアイテム一覧を取得します。</summary>
    /// <param name="author">作者名。</param>
    /// <returns>該当するアイテムのリスト。</returns>
    public List<Item> GetItemsFromAuthor(string author)
    {
        return ItemRepository.GetAll()
            .Where(i => i.Author == author)
            .ToList();
    }

    /// <summary>
    /// 仮アバターを正式なアバターに解決（置換）します。全アイテムの対応アバター・共通素体グループ内のアバターから
    /// 仮アバター ID を正式なアバター ID に置き換えた後、仮アバターを削除します。
    /// </summary>
    /// <param name="tempAvatarId">置換元の仮アバター識別子。</param>
    /// <param name="targetItemId">置換先の正式なアバター識別子。</param>
    public void ResolveTempAvatar(string tempAvatarId, string targetItemId)
    {
        ItemRepository.GetAll()
            .ForEach(i =>
                i.UpdateSupportedAvatars(
                    i.SupportedAvatars
                        .Select(i => i == tempAvatarId ? targetItemId : i)
                        .Distinct()
                )
            );

        ItemRepository.MarkAsChanged();
        ItemRepository.Save();

        CommonAvatarRepository.GetAll()
            .ForEach(c => c.UpdateAvatars(
                c.Avatars
                    .Select(i => i == tempAvatarId ? targetItemId : i)
                    .Distinct()
            ));
        CommonAvatarRepository.MarkAsChanged();
        CommonAvatarRepository.Save();

        _tempAvatars.Remove(tempAvatarId);
        _tempAvatars.Save();
    }

    /// <summary>
    /// アバター ID の一覧を展開し、共通素体グループを構成する個別のアバター ID に変換します。
    /// <paramref name="includeCommonAvatarToSupported"/> が true の場合、共通素体自体も結果に含まれます。
    /// </summary>
    /// <param name="avatars">展開対象のアバター識別子一覧（共通素体を含む）。</param>
    /// <param name="includeCommonAvatarToSupported">共通素体グループ自体を展開後の一覧に含めるかどうか。</param>
    /// <returns>展開された全アバター識別子の配列。</returns>
    public string[] GetAllSupportedAvatarsIds(IEnumerable<string> avatars, bool includeCommonAvatarToSupported = false)
    {
        return AvatarService.GetAllSupportedAvatarIds(avatars, CommonAvatarRepository.GetAll(), includeCommonAvatarToSupported);
    }

    /// <summary>
    /// アイテムを削除し、他のアイテムの対応アバター・実装アバターからもそのIDを削除します。
    /// </summary>
    public void RemoveItem(string identifier, bool removeFolder = false)
    {
        ItemRepository.GetAll()
            .Where(i => i.Identifier != identifier)
            .ForEach(i =>
            {
                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToArray();
                if (updatedSupported.Length != i.SupportedAvatars.Length)
                    i.UpdateSupportedAvatars(updatedSupported);

                var updatedImplemented = i.ImplementedAvatars.Where(a => a != identifier).ToArray();
                if (updatedImplemented.Length != i.ImplementedAvatars.Length)
                    i.UpdateImplementedAvatars(updatedImplemented);
            });
        ItemRepository.Remove(identifier, removeFolder);
        ItemRepository.Save();

        CommonAvatarRepository.GetAll()
            .ForEach(c =>
            {
                var updatedAvatars = c.Avatars.Where(a => a != identifier).ToArray();
                if (updatedAvatars.Length != c.Avatars.Length)
                    c.UpdateAvatars(updatedAvatars);
            });
        CommonAvatarRepository.Save();
        CommonAvatarRepository.MarkAsChanged();
    }

    /// <summary>
    /// 仮アバターを削除し、アイテムの対応アバター・共通素体のアバターからもそのIDを削除します。
    /// </summary>
    public void RemoveTempAvatar(string identifier)
    {
        ItemRepository.GetAll()
            .ForEach(i =>
            {
                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToArray();
                if (updatedSupported.Length != i.SupportedAvatars.Length)
                    i.UpdateSupportedAvatars(updatedSupported);
            });
        ItemRepository.Save();
        ItemRepository.MarkAsChanged();

        CommonAvatarRepository.GetAll()
            .ForEach(c =>
            {
                var updatedAvatars = c.Avatars.Where(a => a != identifier).ToArray();
                if (updatedAvatars.Length != c.Avatars.Length)
                    c.UpdateAvatars(updatedAvatars);
            });
        CommonAvatarRepository.Save();
        CommonAvatarRepository.MarkAsChanged();

        _tempAvatars.Remove(identifier);
        _tempAvatars.Save();
    }

    /// <summary>
    /// 共通素体を削除し、アイテムの対応アバターからもそのIDを削除します。
    /// </summary>
    public void RemoveCommonAvatar(string identifier, bool replaceToAvatars)
    {
        var group = CommonAvatarRepository.Get(identifier);
        if (group == null) return;

        var itemsUpdated = false;

        ItemRepository.GetAll()
            .ForEach(i =>
            {
                var containsGroup = i.SupportedAvatars.Contains(identifier);
                if (!containsGroup) return;

                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToList();
                if (replaceToAvatars) updatedSupported.AddRange(group.Avatars);

                i.UpdateSupportedAvatars(updatedSupported);

                itemsUpdated = true;
            });

        if (itemsUpdated)
        {
            ItemRepository.Save();
            ItemRepository.MarkAsChanged();
        }

        CommonAvatarRepository.Remove(identifier);
        CommonAvatarRepository.Save();
    }

    /// <summary>
    /// 衣装アイテムの対応アバターのうち、指定した共通素体グループに含まれるアバターを、共通素体グループ自体の識別子に置換します。
    /// </summary>
    /// <param name="groupIdentifier">対象の共通素体グループ識別子。</param>
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupIdentifier)
    {
        var commonAvatar = CommonAvatarRepository.Get(groupIdentifier);
        if (commonAvatar == null) return;

        var updatedIdentifiers = new List<string>();
        foreach (var item in ItemRepository.GetAll().Where(i => i.Category.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => commonAvatar.Avatars.Contains(i) ? commonAvatar.Identifier : i).Distinct());
            updatedIdentifiers.Add(item.Identifier);
        }

        ItemRepository.Save();
        ItemRepository.MarkAsChanged();
    }

    #region Search

    /// <summary>
    /// 検索インデックスを再構築します。
    /// アプリ起動時やデータベースを一括で読み込んだ後に呼び出してください。
    /// </summary>
    public void RebuildIndices()
    {
        lock (_indicesLock)
        {
            _itemSearchIndices.Clear();
            _commonAvatarSearchIndices.Clear();
            _tempAvatarSearchIndices.Clear();
            BuildAllIndices();
            _indicesBuilt = true;
        }
    }

    /// <summary>
    /// 検索を実行し、一致した識別子を返します。
    /// </summary>
    /// <param name="searchString">検索文字列。スペース区切りで AND 検索。~ で始まると除外。FieldName="値" でフィールド指定。</param>
    /// <param name="types">検索対象のグループ。</param>
    /// <param name="locKeyProvider">カテゴリ検索時に表示名を LocalizationKey に変換する関数。</param>
    /// <returns>一致したアイテムなどの Identifier 一覧。</returns>
    public string[] SearchItems(string searchString, SearchResultTypes types, Func<string, string>? locKeyProvider = null)
    {
        EnsureIndicesBuilt();

        var query = SearchQueryParser.Parse(searchString);
        var results = new List<(string Identifier, int Score)>();

        if (types.HasFlag(SearchResultTypes.Items))
        {
            foreach (var item in ItemRepository.GetAll())
            {
                if (!query.IncludeHidden && item.IsHidden) continue;
                if (_itemSearchIndices.TryGetValue(item.Identifier, out var index))
                {
                    var score = index.CountMatches(query.Tokens, locKeyProvider);
                    if (query.IsOr ? score > 0 : score == query.Tokens.Count)
                        results.Add((item.Identifier, score));
                }
            }
        }

        if (types.HasFlag(SearchResultTypes.CommonAvatar))
        {
            foreach (var commonAvatar in CommonAvatarRepository.GetAll().Select(i => i.Identifier))
            {
                if (_commonAvatarSearchIndices.TryGetValue(commonAvatar, out var index))
                {
                    var score = index.CountMatches(query.Tokens, locKeyProvider);
                    if (query.IsOr ? score > 0 : score == query.Tokens.Count)
                        results.Add((commonAvatar, score));
                }
            }
        }

        if (types.HasFlag(SearchResultTypes.TempAvatar))
        {
            foreach (var tempAvatar in _tempAvatars.GetAll().Select(i => i.Identifier))
            {
                if (_tempAvatarSearchIndices.TryGetValue(tempAvatar, out var index))
                {
                    var score = index.CountMatches(query.Tokens, locKeyProvider);
                    if (query.IsOr ? score > 0 : score == query.Tokens.Count)
                        results.Add((tempAvatar, score));
                }
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Select(r => r.Identifier)
            .ToArray();
    }

    private void EnsureIndicesBuilt()
    {
        if (_indicesBuilt) return;
        lock (_indicesLock)
        {
            if (_indicesBuilt) return;
            BuildAllIndices();
            _indicesBuilt = true;
        }
    }

    private void BuildAllIndices()
    {
        var avatarTitleMap = ItemUtils.GetItemTitleMaps(ItemRepository.GetAll().Where(i => i.Category.Type == ItemType.Avatar), _tempAvatars.GetAll());
        var commonAvatarList = CommonAvatarRepository.GetAll().ToList();

        foreach (var item in ItemRepository.GetAll())
        {
            BuildItemIndex(item, avatarTitleMap, commonAvatarList);
        }

        foreach (var commonAvatar in CommonAvatarRepository.GetAll())
        {
            var targetItemIndices = commonAvatar.Avatars
                .Select(a => _itemSearchIndices.TryGetValue(a, out var index) ? index : null);
            _commonAvatarSearchIndices[commonAvatar.Identifier] = CommonAvatarSearchIndex.Build(commonAvatar, targetItemIndices);
        }

        foreach (var tempAvatar in _tempAvatars.GetAll())
        {
            _tempAvatarSearchIndices[tempAvatar.Identifier] = TempAvatarSearchIndex.Build(tempAvatar);
        }
    }

    private void BuildItemIndex(Item item, Dictionary<string, string> avatarTitleMap, List<CommonAvatar> commonAvatarList)
    {
        var supportedAvatarIds = AvatarService.GetAllSupportedAvatarIds(
            item.SupportedAvatars, commonAvatarList, includeCommonAvatarToSupported: true);

        var supportedAvatarNames = supportedAvatarIds
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var implementedAvatarNames = item.ImplementedAvatars
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var notImplementedAvatarNames = avatarTitleMap.Keys
            .Except(item.ImplementedAvatars)
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var commonAvatarNames = commonAvatarList
            .Where(ca => ca.Avatars.Any(a => item.SupportedAvatars.Contains(a)))
            .Select(ca => ca.GroupName)
            .ToArray();

        _itemSearchIndices[item.Identifier] = ItemSearchIndex.Build(
            item,
            supportedAvatarNames,
            implementedAvatarNames,
            notImplementedAvatarNames,
            commonAvatarNames);
    }

    private void OnDatabaseUpdated()
    {
        if (!_indicesBuilt) return;
        RebuildIndices();
    }

    #endregion

    /// <summary>指定したリクエストに従って、アイテム・共通素体・仮アバターのデータを CSV または KonoAsset 形式でエクスポートします。</summary>
    /// <param name="exportRequest">エクスポートの形式・出力先・進捗コールバックなどを指定するリクエスト。</param>
    /// <returns>成功した場合は <see cref="Success"/>、失敗した場合はエラー情報。</returns>
    public async Task<ErrorOr<Success>> Export(ExportRequest exportRequest)
    {
        var exportContext = new ExportContext()
        {
            Items = ItemRepository.GetAll(),
            CommonAvatars = CommonAvatarRepository.GetAll(),
            TempAvatars = _tempAvatars.GetAll(),
            RuntimeSettings = _runtimesettings.Settings
        };

        return await DataExporter.Export(exportContext, exportRequest);
    }

    /// <summary>指定したリクエストに従って、外部データ（V1・KonoAsset・フォルダ）からアイテムとサムネイルをインポートします。</summary>
    /// <param name="importRequest">インポート元の種別・データフォルダ・コピー動作・進捗コールバックなどを指定するリクエスト。</param>
    /// <returns>成功した場合は <see cref="Success"/>、失敗した場合はエラー情報。</returns>
    public async Task<ErrorOr<Success>> Import(ImportRequest importRequest)
    {
        var importer = new DataImporter(ItemRepository, CommonAvatarRepository, _tempAvatars);
        return await importer.Import(importRequest);
    }
}
