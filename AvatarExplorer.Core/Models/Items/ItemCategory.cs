using System.Text.Json.Serialization;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// アイテムのカテゴリを表すレコードです。組み込みの <see cref="ItemType"/> またはカスタムカテゴリ名のいずれかを保持します。
/// </summary>
public record ItemCategory : IIdentifiable
{
    /// <summary>組み込みのカテゴリタイプです。カスタムカテゴリの場合は <see cref="ItemType.Custom"/> になります。</summary>
    public ItemType Type { get; init; } = ItemType.None;

    /// <summary>カスタムカテゴリ名です。組み込みタイプの場合は空文字になります。</summary>
    public string CustomCategory { get; init; } = string.Empty;

    /// <summary>カスタムカテゴリの識別子プレフィックス（"custom:"）です。</summary>
    public const string CustomCategoryPrefix = "custom:";

    /// <summary>組み込みタイプカテゴリの識別子プレフィックス（"type:"）です。</summary>
    public const string TypeCategoryPrefix = "type:";

    /// <summary>このカテゴリがローカライズ可能かどうかを示します。<see cref="ItemType.Custom"/> および <see cref="ItemType.None"/> 以外の場合は true です。</summary>
    [JsonIgnore] public bool IsLocalizable => Type != ItemType.Custom && Type != ItemType.None;

    /// <summary>指定した識別子がカテゴリ識別子（"type:" または "custom:" で始まる）かどうかを判定します。</summary>
    /// <param name="identifier">判定対象の識別子。</param>
    /// <returns>カテゴリ識別子の場合は true、それ以外は false。</returns>
    public static bool IsCategoryIdentifier(string identifier) => identifier.StartsWith(CustomCategoryPrefix) || identifier.StartsWith(TypeCategoryPrefix);

    /// <summary>識別子から対応する <see cref="ItemCategory"/> を生成します。解析できない場合は <see cref="ItemType.None"/> のカテゴリを返します。</summary>
    /// <param name="identifier">"type:{数値}" または "custom:{名前}" 形式の識別子。</param>
    /// <returns>生成された <see cref="ItemCategory"/>。</returns>
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

        return None;
    }

    #region Constructor
    /// <summary><see cref="ItemType.None"/> を持つ空のカテゴリを初期化します。</summary>
    public ItemCategory()
    {
    }

    /// <summary>既存の <see cref="ItemCategory"/> をコピーして新しいカテゴリを初期化します。</summary>
    /// <param name="category">コピー元のカテゴリ。</param>
    public ItemCategory(ItemCategory category)
    {
        Type = category.Type;
        CustomCategory = category.CustomCategory;
    }

    /// <summary>指定した組み込みタイプ（および任意のカスタムカテゴリ名）を持つカテゴリを初期化します。customCategory が空でない場合は <see cref="ItemType.Custom"/> として扱われます。</summary>
    /// <param name="type">組み込みカテゴリタイプ。</param>
    /// <param name="customCategory">カスタムカテゴリ名。空の場合は type がそのまま使用されます。</param>
    public ItemCategory(ItemType type, string customCategory = "")
    {
        Type = string.IsNullOrEmpty(customCategory) ? type : ItemType.Custom;
        CustomCategory = customCategory;
    }

    /// <summary>指定したカスタムカテゴリ名を持つカテゴリ（<see cref="ItemType.Custom"/>）を初期化します。</summary>
    /// <param name="customCategory">カスタムカテゴリ名。</param>
    public ItemCategory(string customCategory)
    {
        Type = ItemType.Custom;
        CustomCategory = customCategory;
    }
    #endregion

    /// <summary>カテゴリの表示名を返します。カスタムカテゴリの場合はその名前、それ以外はローカライズキー（または enum 名）になります。</summary>
    /// <returns>カテゴリの表示名。</returns>
    public override string ToString() => Type == ItemType.Custom ? CustomCategory : (Type.GetLocalizationKey() ?? Type.ToString());

    /// <summary>このカテゴリの識別子（"type:{数値}" または "custom:{名前}"）です。</summary>
    [JsonIgnore] public string Identifier => Type == ItemType.Custom ?
        (CustomCategoryPrefix + CustomCategory) :
        (TypeCategoryPrefix + (int)Type);

    [JsonIgnore] public static readonly ItemCategory None = new(ItemType.None);
    [JsonIgnore] public static readonly ItemCategory Avatar = new(ItemType.Avatar);
    [JsonIgnore] public static readonly ItemCategory Clothing = new(ItemType.Clothing);
    [JsonIgnore] public static readonly ItemCategory Texture = new(ItemType.Texture);
    [JsonIgnore] public static readonly ItemCategory Gimmick = new(ItemType.Gimmick);
    [JsonIgnore] public static readonly ItemCategory Accessory = new(ItemType.Accessory);
    [JsonIgnore] public static readonly ItemCategory HairStyle = new(ItemType.HairStyle);
    [JsonIgnore] public static readonly ItemCategory Animation = new(ItemType.Animation);
    [JsonIgnore] public static readonly ItemCategory Tool = new(ItemType.Tool);
    [JsonIgnore] public static readonly ItemCategory Shader = new(ItemType.Shader);
    [JsonIgnore] public static readonly ItemCategory All = new(ItemType.All);
    [JsonIgnore] public static readonly ItemCategory Hidden = new(ItemType.Hidden);
}
