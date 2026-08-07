using System.Text.Json.Serialization;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public record ItemCategory : IIdentifiable
{
    public ItemType Type { get; init; } = ItemType.None;
    public string CustomCategory { get; init; } = string.Empty;
    
    [JsonIgnore] public string Identifier => Type == ItemType.Custom ? $"custom:{CustomCategory}" : $"type:{(int)Type}";
    [JsonIgnore] public bool IsLocalizable => Type != ItemType.Custom && Type != ItemType.None;

    #region Constructor
    public ItemCategory()
    {
    }

    public ItemCategory(ItemCategory category)
    {
        Type = category.Type;
        CustomCategory = category.CustomCategory;
    }

    public ItemCategory(ItemType type, string customCategory = "")
    {
        Type = string.IsNullOrEmpty(customCategory) ? type : ItemType.Custom;
        CustomCategory = customCategory;
    }

    public ItemCategory(string customCategory)
    {
        Type = ItemType.Custom;
        CustomCategory = customCategory;
    }
    #endregion

    public override string ToString() => Type == ItemType.Custom ? CustomCategory : (Type.GetLocalizationKey() ?? Type.ToString());
}
