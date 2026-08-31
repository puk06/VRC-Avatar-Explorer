using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// アイテムの情報を保持するモデルクラスです。ナビゲーション・検索・編集の対象となります。
/// </summary>
public class Item : AbstractDatabaseItem, IIdentifiable
{
    /// <summary>アイテムの名前（タイトル）です。</summary>
    [JsonInclude] public string Title { get; private set; } = string.Empty;
    /// <summary>作者名です。</summary>
    [JsonInclude] public string Author { get; private set; } = string.Empty;
    /// <summary>作者のBoothサブドメインIDです。</summary>
    [JsonInclude] public string AuthorId { get; private set; } = string.Empty;
    /// <summary>Boothの商品IDです。未設定の場合は -1 になります。</summary>
    [JsonInclude] public int BoothId { get; private set; } = -1;

    /// <summary>
    /// アイテムのパスです。相対パスの可能性もあるため、フルパスを取得するには item.GetItemPath() を使用してください。
    /// </summary>
    [JsonInclude] public string ItemPath { get; private set; } = string.Empty; // デフォルトの展開先（item.GetItemPath()でフルパスに変換する）
    /// <summary>追加のフォルダパス一覧です。フォルダをそのまま使用する設定の場合にここへ追加されます。</summary>
    [JsonInclude] public ImmutableArray<string> ItemPaths { get; private set; } = []; // フォルダー（フォルダーをそのまま使用する設定の時にここに追加される）
    /// <summary>サムネイル画像のファイル名です。</summary>
    [JsonInclude] public string ThumbnailFileName { get; private set; } = string.Empty;
    /// <summary>アイテムのカテゴリ（組み込みタイプとカスタムカテゴリ）です。</summary>
    [JsonInclude] public ItemCategory Category { get; private set; } = ItemCategory.Get(ItemType.None);
    /// <summary>対応アバターの識別子（Identifier）一覧です。</summary>
    [JsonInclude] public ImmutableArray<string> SupportedAvatars { get; private set; } = [];
    /// <summary>実装済みアバターの識別子（Identifier）一覧です。</summary>
    [JsonInclude] public ImmutableArray<string> ImplementedAvatars { get; private set; } = [];
    /// <summary>タグの一覧です。</summary>
    [JsonInclude] public ImmutableArray<string> Tags { get; private set; } = [];
    /// <summary>アイテムのメモです。</summary>
    [JsonInclude] public string ItemMemo { get; private set; } = string.Empty;
    /// <summary>作成日時（Unixタイムスタンプ）です。</summary>
    [JsonInclude] public string CreatedDate { get; private set; } = string.Empty;
    /// <summary>更新日時（Unixタイムスタンプ）です。</summary>
    [JsonInclude] public string UpdatedDate { get; private set; } = string.Empty;
    /// <summary>非表示フラグです。true の場合は一覧などに表示されません。</summary>
    [JsonInclude] public bool IsHidden { get; private set; } = false;
    /// <summary>アイテムを共通素体チェックから外すかどうかのフラグです。trueの場合、間接的な共通素体グループ経由の対応判定がされなくなります。対応アバターに共通素体グループが直接設定されている場合は、このフラグに関わらず判定されます。</summary>
    [JsonInclude] public bool SkipIndirectCommonAvatarCheck { get; private set; } = false;

    /// <summary>アイテムの基本メタデータ（タイトル・作者・カテゴリ・メモ等）を一括で更新します。</summary>
    /// <param name="title">新しいタイトル。</param>
    /// <param name="author">新しい作者名。</param>
    /// <param name="authorId">新しい作者ID。</param>
    /// <param name="boothId">新しいBooth商品ID。</param>
    /// <param name="category">新しいカテゴリ。</param>
    /// <param name="itemMemo">新しいメモ。</param>
    public void UpdateMetadata(string title, string author, string authorId, int boothId, ItemCategory category, string itemMemo)
    {
        Title = title;
        Author = author;
        AuthorId = authorId;
        BoothId = boothId;
        Category = category;
        ItemMemo = itemMemo;
    }

    /// <summary>タイトルを更新します。</summary>
    /// <param name="title">新しいタイトル。</param>
    public void UpdateTitle(string title) => Title = title;
    /// <summary>作者名を更新します。</summary>
    /// <param name="author">新しい作者名。</param>
    public void UpdateAuthor(string author) => Author = author;
    /// <summary>作者ID（Boothのsubdomain）を更新します。</summary>
    /// <param name="authorId">新しい作者ID。</param>
    public void UpdateAuthorId(string authorId) => AuthorId = authorId;
    /// <summary>Booth商品IDを更新します。</summary>
    /// <param name="boothId">新しいBooth商品ID。</param>
    public void UpdateBoothId(int boothId) => BoothId = boothId;
    /// <summary>カテゴリを更新します。</summary>
    /// <param name="category">新しいカテゴリ。</param>
    public void UpdateCategory(ItemCategory category) => Category = category;
    /// <summary>メモを更新します。</summary>
    /// <param name="memo">新しいメモ。</param>
    public void UpdateMemo(string memo) => ItemMemo = memo;

    /// <summary>アイテムのルートパスを更新します。</summary>
    /// <param name="itemPath">新しいルートパス。</param>
    public void UpdateItemPath(string itemPath) => ItemPath = itemPath;
    /// <summary>追加のフォルダパス一覧を更新します。重複は除去されます。</summary>
    /// <param name="newList">新しいフォルダパス一覧。</param>
    public void UpdateItemPaths(IEnumerable<string> newList) => ItemPaths = newList.Distinct().ToImmutableArray();
    /// <summary>サムネイルファイル名を更新します。</summary>
    /// <param name="fileName">新しいサムネイルファイル名。</param>
    public void UpdateThumbnailFileName(string fileName) => ThumbnailFileName = fileName;
    /// <summary>対応アバター一覧を更新します。重複は除去されます。</summary>
    /// <param name="newList">新しい対応アバターの識別子一覧。</param>
    public void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.Distinct().ToImmutableArray();
    /// <summary>実装済みアバター一覧を更新します。重複は除去されます。</summary>
    /// <param name="newList">新しい実装済みアバターの識別子一覧。</param>
    public void UpdateImplementedAvatars(IEnumerable<string> newList) => ImplementedAvatars = newList.Distinct().ToImmutableArray();
    /// <summary>タグ一覧を更新します。重複は除去されます。</summary>
    /// <param name="newList">新しいタグ一覧。</param>
    public void UpdateTags(IEnumerable<string> newList) => Tags = newList.Distinct().ToImmutableArray();
    /// <summary>作成日時と更新日時を設定します。</summary>
    /// <param name="createdDate">作成日時（Unixタイムスタンプ）。</param>
    /// <param name="updatedDate">更新日時（Unixタイムスタンプ）。</param>
    public void SetCreationDates(string createdDate, string updatedDate)
    {
        CreatedDate = createdDate;
        UpdatedDate = updatedDate;
    }
    /// <summary>更新日時を設定します。</summary>
    /// <param name="updatedDate">更新日時（Unixタイムスタンプ）。</param>
    public void UpdateTimestamp(string updatedDate) => UpdatedDate = updatedDate;
    /// <summary>非表示フラグを更新します。</summary>
    /// <param name="isHidden">非表示にする場合は true。</param>
    public void UpdateIsHidden(bool isHidden) => IsHidden = isHidden;
    /// <summary>共通素体チェック除外フラグを更新します。</summary>
    /// <param name="exclude">共通素体チェックから外す場合は true。</param>
    public void UpdateSkipIndirectCommonAvatarCheck(bool exclude) => SkipIndirectCommonAvatarCheck = exclude;

    /// <summary>指定した言語コードを用いて、このアイテムのBooth商品ページへのリンクを生成して返します。</summary>
    /// <param name="languageCode">Boothリンクに使用する言語コード（例: "ja"）。</param>
    /// <returns>AuthorIdが設定されている場合は "https://{subdomain}.booth.pm/items/{id}"、それ以外は "https://booth.pm/{lang}/items/{id}" の形式のURL。</returns>
    public string GetBoothLink(string languageCode)
    {
        if (string.IsNullOrEmpty(AuthorId)) return string.Format(BoothLink.ItemURLWithoutAuthorFormat, languageCode, BoothId);
        else return string.Format(BoothLink.ItemURLFormat, AuthorId, BoothId);
    }

    /// <summary>このアイテムを一意に識別する識別子（"item:" + Id）です。</summary>
    [JsonIgnore] public string Identifier => "item:" + Id;
}
