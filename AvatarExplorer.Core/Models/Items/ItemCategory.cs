using System.Text.Json.Serialization;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public record ItemCategory : IIdentifiable
{
    public ItemType Type { get; init; } = ItemType.None;
    public string CustomCategory { get; init; } = string.Empty;

    public const string CustomCategoryPrefix = "custom:";
    public const string TypeCategoryPrefix = "type:";
    
    [JsonIgnore] public string Identifier => Type == ItemType.Custom ?
        (CustomCategoryPrefix + CustomCategory) :
        (TypeCategoryPrefix + (int)Type);

    [JsonIgnore] public bool IsLocalizable => Type != ItemType.Custom && Type != ItemType.None;

    public static bool IsCategoryIdentifier(string identifier) => identifier.StartsWith(CustomCategoryPrefix) || identifier.StartsWith(TypeCategoryPrefix);
    public static ItemCategory FromIdentifier(string identifier)
    {
        if (identifier.StartsWith(CustomCategoryPrefix))
        {
            return new ItemCategory(identifier[CustomCategoryPrefix.Length..]);
        }
        else if (identifier.StartsWith(TypeCategoryPrefix))
        {
            var typeString = identifier[TypeCategoryPrefix.Length..];
            if (int.TryParse(typeString, out int typeValue) && Enum.IsDefined(typeof(ItemType), typeValue))
            {
                return new ItemCategory((ItemType)typeValue);
            }
        }

        return new ItemCategory(ItemType.None);
    }

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
