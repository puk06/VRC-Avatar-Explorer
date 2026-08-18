using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class Item : AbstractDatabaseItem, IIdentifiable
{
    [JsonInclude] public string Title { get; private set; } = string.Empty;
    [JsonInclude] public string Author { get; private set; } = string.Empty;
    [JsonInclude] public string AuthorId { get; private set; } = string.Empty;
    [JsonInclude] public int BoothId { get; private set; } = -1;
    [JsonInclude] public string ItemPath { get; private set; } = string.Empty; // デフォルトの展開先（item.GetItemPath()でフルパスに変換する）
    [JsonInclude] public ImmutableArray<string> ItemPaths { get; private set; } = []; // フォルダー（フォルダーをそのまま使用する設定の時にここに追加される）
    [JsonInclude] public string ThumbnailFileName { get; private set; } = string.Empty;
    [JsonInclude] public ItemCategory Category { get; private set; } = new(ItemType.None);
    [JsonInclude] public ImmutableArray<string> SupportedAvatars { get; private set; } = [];
    [JsonInclude] public ImmutableArray<string> ImplementedAvatars { get; private set; } = [];
    [JsonInclude] public ImmutableArray<string> Tags { get; private set; } = [];
    [JsonInclude] public string ItemMemo { get; private set; } = string.Empty;
    [JsonInclude] public string CreatedDate { get; private set; } = string.Empty;
    [JsonInclude] public string UpdatedDate { get; private set; } = string.Empty;

    public void UpdateMetadata(string title, string author, string authorId, int boothId, ItemCategory category, string itemMemo)
    {
        Title = title;
        Author = author;
        AuthorId = authorId;
        BoothId = boothId;
        Category = new ItemCategory(category);
        ItemMemo = itemMemo;
    }

    public void UpdateTitle(string title) => Title = title;
    public void UpdateAuthor(string author) => Author = author;
    public void UpdateAuthorId(string authorId) => AuthorId = authorId;
    public void UpdateBoothId(int boothId) => BoothId = boothId;
    public void UpdateCategory(ItemCategory category) => Category = new ItemCategory(category);
    public void UpdateMemo(string memo) => ItemMemo = memo;

    public void UpdateItemPath(string itemPath) => ItemPath = itemPath;
    public void UpdateItemPaths(IEnumerable<string> newList) => ItemPaths = newList.Distinct().ToImmutableArray();
    public void UpdateThumbnailFileName(string fileName) => ThumbnailFileName = fileName;
    public void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.Distinct().ToImmutableArray();
    public void UpdateImplementedAvatars(IEnumerable<string> newList) => ImplementedAvatars = newList.Distinct().ToImmutableArray();
    public void UpdateTags(IEnumerable<string> newList) => Tags = newList.Distinct().ToImmutableArray();
    public void SetCreationDates(string createdDate, string updatedDate)
    {
        CreatedDate = createdDate;
        UpdatedDate = updatedDate;
    }
    public void UpdateTimestamp(string updatedDate) => UpdatedDate = updatedDate;

    public string GetBoothLink(string languageCode)
    {
        if (string.IsNullOrEmpty(AuthorId)) return string.Format(BoothLink.ItemURLWithoutAuthorFormat, languageCode, BoothId);
        else return string.Format(BoothLink.ItemURLFormat, AuthorId, BoothId);
    }
    
    [JsonIgnore] public string Identifier => "item:" + Id;
}
