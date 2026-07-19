using AvatarExplorer.Core.Extensions;

namespace AvatarExplorer.Core.Models.Items;

public class ItemCategory
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

    public override string ToString() => Type == ItemType.Custom ? CustomCategory : (Type.GetLocalizationKey() ?? Type.ToString());
    
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
