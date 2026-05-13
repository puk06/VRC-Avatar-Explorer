using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Data.Mappings;

internal static class BoothMapping
{
    internal static readonly Dictionary<string[], ItemType> TitleMappings = new()
    {
        { new[] { "オリジナル3Dモデル", "オリジナル", "Avatar", "Original" }, ItemType.Avatar },
        { new[] { "アニメーション", "Animation" }, ItemType.Animation },
        { new[] { "衣装", "Clothing" }, ItemType.Clothing },
        { new[] { "ギミック", "Gimmick" }, ItemType.Gimmick },
        { new[] { "アクセサリ", "Accessory" }, ItemType.Accessory },
        { new[] { "髪", "Hair" }, ItemType.HairStyle },
        { new[] { "テクスチャ", "Eye", "Texture" }, ItemType.Texture },
        { new[] { "ツール", "システム", "Tool", "System" }, ItemType.Tool },
        { new[] { "シェーダー", "Shader" }, ItemType.Shader }
    };

    internal static readonly Dictionary<string, ItemType> CategoryMappings = new()
    {
        // Category : 3D Models
        { "3D Characters", ItemType.Avatar }, // 3Dキャラクター
        { "3D Clothing", ItemType.Clothing }, // 3D衣装
        { "3D Hair", ItemType.HairStyle }, // 3D 髪型
        { "3D Accessories", ItemType.Accessory }, // 3D装飾品
        { "3D Shoes", ItemType.Clothing }, // 3D 靴
        { "3D Props", ItemType.Gimmick }, // 3D小道具
        { "3D Textures", ItemType.Texture }, // 3Dテクスチャ
        { "3D Tools & Systems", ItemType.Tool }, // 3Dツール・システム
        { "3D Motion & Animation", ItemType.Animation }, // 3Dモーション・アニメーション
        { "3D Models (Other)" , ItemType.Avatar }, // 3Dモデル（その他）

        // Category : Software & Hardware
        { "Software", ItemType.Tool } // ソフトウェア
    };
}
