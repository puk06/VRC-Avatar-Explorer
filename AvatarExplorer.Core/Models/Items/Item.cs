using System.Text.Json.Serialization;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Interfaces.Database;

namespace AvatarExplorer.Core.Models.Items;

public class Item : ISelectableItem, IDatabaseItem
{
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int BoothId { get; set; } = -1;
    public string ItemPath { get; set; } = string.Empty;
    public string ThumbnmailFileName { get; set; } = string.Empty;
    public string AuthorThumbnmailFileName { get; set; } = string.Empty;
    public ItemType Type { get; set; } = ItemType.None;
    public string CustomCategory { get; set; } = string.Empty;
    [JsonInclude] private List<string> SupportedAvatars { get; set; } = new List<string>();
    [JsonInclude] private List<string> ImplementedAvatars { get; set; } = new List<string>();
    [JsonInclude] private List<string> Tags { get; set; } = new List<string>();
    public string ItemMemo { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;

    [JsonIgnore] public IReadOnlyList<string> SupportedAvatarsView => SupportedAvatars;
    [JsonIgnore] public IReadOnlyList<string> ImplementedAvatarsView => ImplementedAvatars;
    [JsonIgnore] public IReadOnlyList<string> TagsView => Tags;

    public void UpdateSupportedAvatars(IEnumerable<string> newList) => SupportedAvatars = newList.ToList();
    public void UpdateImplementedAvatars(IEnumerable<string> newList) => ImplementedAvatars = newList.ToList();
    public void UpdateTags(IEnumerable<string> newList) => Tags = newList.ToList();
    
    public string GetBoothLink() => string.Format(BoothLink.ItemURLFormat, AuthorId, BoothId);
    
    internal Item SetValuesFromCreationContext(ItemCreationContext itemCreationContext)
    {
        Title = itemCreationContext.Title;
        Author = itemCreationContext.Author;
        AuthorId = itemCreationContext.AuthorId;
        BoothId = itemCreationContext.BoothId;
        Type = itemCreationContext.ItemType;
        CustomCategory = itemCreationContext.CustomCategory;
        UpdateSupportedAvatars(itemCreationContext.SupportedAvatars);

        return this;
    }
}
