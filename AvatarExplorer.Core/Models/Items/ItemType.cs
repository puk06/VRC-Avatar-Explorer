using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum ItemType
{
    [NonSelectable]
    None,

    [LocalizationKey(Loc.ItemCategory.Avatar)]
    Avatar,

    [LocalizationKey(Loc.ItemCategory.Clothing)]
    Clothing,

    [LocalizationKey(Loc.ItemCategory.Texture)]
    Texture,

    [LocalizationKey(Loc.ItemCategory.Gimmick)]
    Gimmick,

    [LocalizationKey(Loc.ItemCategory.Accessory)]
    Accessory,

    [LocalizationKey(Loc.ItemCategory.HairStyle)]
    HairStyle,

    [LocalizationKey(Loc.ItemCategory.Animation)]
    Animation,

    [LocalizationKey(Loc.ItemCategory.Tool)]
    Tool,

    [LocalizationKey(Loc.ItemCategory.Shader)]
    Shader,

    [NonSelectable]
    Custom,

    [NonSelectable]
    [LocalizationKey(Loc.ItemCategory.All)]
    All,

    [NonSelectable]
    [LocalizationKey(Loc.ItemCategory.Hidden)]
    Hidden
}
