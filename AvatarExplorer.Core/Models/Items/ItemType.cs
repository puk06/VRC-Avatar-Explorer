using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>アイテムのタイプ（カテゴリ）を表す列挙型です。</summary>
public enum ItemType
{
    /// <summary>未設定。</summary>
    [NonSelectable]
    None,

    /// <summary>アバター。</summary>
    [LocalizationKey(Loc.ItemCategory.Avatar)]
    Avatar,

    /// <summary>衣装。</summary>
    [LocalizationKey(Loc.ItemCategory.Clothing)]
    Clothing,

    /// <summary>テクスチャ。</summary>
    [LocalizationKey(Loc.ItemCategory.Texture)]
    Texture,

    /// <summary>ギミック。</summary>
    [LocalizationKey(Loc.ItemCategory.Gimmick)]
    Gimmick,

    /// <summary>アクセサリー。</summary>
    [LocalizationKey(Loc.ItemCategory.Accessory)]
    Accessory,

    /// <summary>髪型。</summary>
    [LocalizationKey(Loc.ItemCategory.HairStyle)]
    HairStyle,

    /// <summary>アニメーション。</summary>
    [LocalizationKey(Loc.ItemCategory.Animation)]
    Animation,

    /// <summary>ツール。</summary>
    [LocalizationKey(Loc.ItemCategory.Tool)]
    Tool,

    /// <summary>シェーダー。</summary>
    [LocalizationKey(Loc.ItemCategory.Shader)]
    Shader,

    /// <summary>カスタムカテゴリ。</summary>
    [NonSelectable]
    Custom,

    /// <summary>すべて（フィルタ用）。</summary>
    [NonSelectable]
    [LocalizationKey(Loc.ItemCategory.All)]
    All,

    /// <summary>非表示（フィルタ用）。</summary>
    [NonSelectable]
    [LocalizationKey(Loc.ItemCategory.Hidden)]
    Hidden
}
