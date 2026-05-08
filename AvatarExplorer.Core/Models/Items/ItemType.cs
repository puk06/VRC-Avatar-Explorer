using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum ItemType
{
    [NonSelectable]
    None,

    [LocalizationKey(LocalizationKey.ItemCategory.Avatar)]
    Avatar,

    [LocalizationKey(LocalizationKey.ItemCategory.Clothing)]
    Clothing,

    [LocalizationKey(LocalizationKey.ItemCategory.Texture)]
    Texture,

    [LocalizationKey(LocalizationKey.ItemCategory.Gimmick)]
    Gimmick,

    [LocalizationKey(LocalizationKey.ItemCategory.Accessory)]
    Accessory,

    [LocalizationKey(LocalizationKey.ItemCategory.HairStyle)]
    HairStyle,

    [LocalizationKey(LocalizationKey.ItemCategory.Animation)]
    Animation,

    [LocalizationKey(LocalizationKey.ItemCategory.Tool)]
    Tool,

    [LocalizationKey(LocalizationKey.ItemCategory.Shader)]
    Shader,

    [NonSelectable]
    Custom,

    [NonSelectable]
    [LocalizationKey(LocalizationKey.ItemCategory.All)]
    All,
}
