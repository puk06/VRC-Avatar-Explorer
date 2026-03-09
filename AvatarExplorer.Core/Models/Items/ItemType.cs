using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum ItemType
{
    None = -1,

    [LocalizationKey(LocalizationKey.ItemCategory.Avatar)]
    Avatar = 0,

    [LocalizationKey(LocalizationKey.ItemCategory.Clothing)]
    Clothing = 1,

    [LocalizationKey(LocalizationKey.ItemCategory.Texture)]
    Texture = 2,

    [LocalizationKey(LocalizationKey.ItemCategory.Gimmick)]
    Gimmick = 3,

    [LocalizationKey(LocalizationKey.ItemCategory.Accessory)]
    Accessory = 4,

    [LocalizationKey(LocalizationKey.ItemCategory.HairStyle)]
    HairStyle = 5,

    [LocalizationKey(LocalizationKey.ItemCategory.Animation)]
    Animation = 6,

    [LocalizationKey(LocalizationKey.ItemCategory.Tool)]
    Tool = 7,

    [LocalizationKey(LocalizationKey.ItemCategory.Shader)]
    Shader = 8,

    Custom = 9
}
