using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class Item : AbstractDatabaseItem, ISelectableItem
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int BoothId { get; set; } = -1;
    public string ItemPath { get; set; } = string.Empty; // TODO: フルパスに変更する 追加時だけRuntimeSettingsのDataRootは使う
    [JsonInclude] public ImmutableArray<string> ItemPaths { get; private set; } = []; // フォルダーをそのまま使用する設定のときにここに追加される
    public string ThumbnailFileName { get; set; } = string.Empty;
    public ItemType Type { get; set; } = ItemType.None;
    public string CustomCategory { get; set; } = string.Empty;
    [JsonInclude] public ImmutableArray<string> SupportedAvatars { get; private set; } = [];
    [JsonInclude] public ImmutableArray<string> ImplementedAvatars { get; private set; } = [];
    [JsonInclude] public ImmutableArray<string> Tags { get; private set; } = [];
    public string ItemMemo { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;

    [JsonIgnore] public string Identifier => "item:" + Id;

    public void UpdateItemPaths(IEnumerable<string> newList) => ItemPaths = newList.ToImmutableArray();
    public void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.ToImmutableArray();
    public void UpdateImplementedAvatars(IEnumerable<string> newList) => ImplementedAvatars = newList.ToImmutableArray();
    public void UpdateTags(IEnumerable<string> newList) => Tags = newList.ToImmutableArray();
    
    public string GetBoothLink(string languageCode)
    {
        if (string.IsNullOrEmpty(AuthorId)) return string.Format(BoothLink.ItemURLWithoutAuthorFormat, languageCode, BoothId);
        else return string.Format(BoothLink.ItemURLFormat, AuthorId, BoothId);
    }
}
