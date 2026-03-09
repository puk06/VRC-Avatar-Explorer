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
        { "3Dキャラクター", ItemType.Avatar },
        { "3Dモデル（その他）" , ItemType.Avatar },
        { "3Dモーション・アニメーション", ItemType.Animation },
        { "3D衣装", ItemType.Clothing },
        { "3D小道具", ItemType.Gimmick },
        { "3D装飾品", ItemType.Accessory },
        { "3Dテクスチャ", ItemType.Texture },
        { "3Dツール・システム", ItemType.Tool }
    };
}
