using AvatarExplorer.Core.Attributes;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// 列挙型のメンバーに付与された属性を取得したり、選択可否を判定したりする拡張メソッドを提供します。
/// </summary>
public static class EnumExtensions
{
    private const char FilterSplitter = '|';
    /// <summary>列挙値に付与された <see cref="LocalizationKeyAttribute"/> のローカライズキーを取得します。</summary>
    /// <param name="value">対象の列挙値。</param>
    /// <returns>ローカライズキー。属性がない場合は null。</returns>
    public static string? GetLocalizationKey(this Enum value) => value.GetAttribute<LocalizationKeyAttribute>()?.Key;
    /// <summary>列挙値に付与された <see cref="ExtensionsFilterAttribute"/> の拡張子フィルタを分割して取得します。</summary>
    /// <param name="value">対象の列挙値。</param>
    /// <returns>拡張子フィルタの配列。属性がない場合は null。</returns>
    public static string[]? GetExtensionFilters(this Enum value) => value.GetAttribute<ExtensionsFilterAttribute>()?.Filter.Split(FilterSplitter) ?? null;
    /// <summary>列挙値に付与された <see cref="FileNamesFilterAttribute"/> のファイル名フィルタを分割して取得します。</summary>
    /// <param name="value">対象の列挙値。</param>
    /// <returns>ファイル名フィルタの配列。属性がない場合は null。</returns>
    public static string[]? GetFileNameFilters(this Enum value) => value.GetAttribute<FileNamesFilterAttribute>()?.Filter.Split(FilterSplitter) ?? null;
    /// <summary>列挙値が <see cref="NonSelectableAttribute"/> を持たない（選択可能である）かどうかを判定します。</summary>
    /// <param name="value">対象の列挙値。</param>
    /// <returns>選択可能な場合は true。</returns>
    public static bool IsSelectable(this Enum value) => value.GetAttribute<NonSelectableAttribute>() == null;

    /// <summary>
    /// 列挙値に付与された指定の型の属性を取得します。
    /// </summary>
    /// <typeparam name="T">取得する属性の型。</typeparam>
    /// <param name="value">対象の列挙値。</param>
    /// <returns>見つかった属性。存在しない場合は null。</returns>
    public static T? GetAttribute<T>(this Enum value) where T : class
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }
}
