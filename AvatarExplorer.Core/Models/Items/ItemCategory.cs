using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class ItemCategory : ISelectableItem
{
    public ItemType Type { get; private set; } = ItemType.None;
    public string CustomCategory { get; private set; } = string.Empty;
    
    public bool IsEmpty => Type == ItemType.None && CustomCategory == string.Empty;
    public string CategoryName => Type == ItemType.Custom ? CustomCategory : Type.ToString();
    public string LocalizationKey => Type == ItemType.Custom ? string.Empty : (Type.GetLocalizationKey() ?? string.Empty);

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

    #region Set API
    public void SetCategory(ItemCategory category)
    {
        Type = category.Type;
        CustomCategory = category.CustomCategory;
    }

    public void SetCategory(ItemType type, string customCategory = "")
    {
        Type = string.IsNullOrEmpty(customCategory) ? type : ItemType.Custom;
        CustomCategory = customCategory;
    }
    
    public void SetCategory(string customCategory)
    {
        Type = ItemType.Custom;
        CustomCategory = customCategory;
    }
    #endregion

    public override string ToString() => Type == ItemType.Custom ? CustomCategory : (Type.GetLocalizationKey() ?? Type.ToString());

    private const string CustomCategoryPrefix = "<sys:customcategory>";
    private const string ItemCategoryPrefix = "<sys:itemcategory>";
    public string GetInternalId() => Type == ItemType.Custom ? CustomCategoryPrefix + CustomCategory : ItemCategoryPrefix + Type.ToString();

    public static bool TryParse(string internalId, out ItemCategory? category)
    {
        category = default;

        if (internalId.StartsWith(CustomCategoryPrefix))
        {
            category = new(internalId[CustomCategoryPrefix.Length..]);
            return true;
        }
        else if (internalId.StartsWith(ItemCategoryPrefix))
        {
            var type = internalId[ItemCategoryPrefix.Length..];
            if (Enum.TryParse(type, out ItemType itemType))
            {
                category = new(itemType);
                return true;
            }
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ItemCategory other)
        {
            return Type == other.Type && CustomCategory == other.CustomCategory;
        }
        
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, CustomCategory);
    }
}
