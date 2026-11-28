using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

/// <summary>
/// アイテム内のファイルを、拡張子などのルールに基づいて <see cref="ItemFileCategoryType"/> ごとに分類するためのユーティリティクラスです。
/// </summary>
public static class FileCategorizer
{
    private sealed record CategoryRule(ItemFileCategoryType Category, HashSet<string>? Extensions, string[]? FileNames);

    // 分類ルールは起動中に変化しないため、属性の読み取りと分割は一度だけ行う。
    private static readonly CategoryRule[] Rules = Enum.GetValues<ItemFileCategoryType>()
        .Where(c => c != ItemFileCategoryType.None && c != ItemFileCategoryType.Unknown)
        .Select(category => new CategoryRule(
            category,
            category.GetExtensionFilters() is { } extensions
                ? new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase)
                : null,
            category.GetFileNameFilters()))
        .ToArray();

    /// <summary>
    /// 指定したファイルの列挙可能なコレクションを、カテゴリ種別（<see cref="ItemFileCategoryType"/>）ごとのリストに分類します。
    /// </summary>
    /// <param name="files">分類対象のファイル一覧。</param>
    /// <returns>カテゴリ種別をキーとし、該当するファイルのリストを値とする辞書。</returns>
    public static Dictionary<ItemFileCategoryType, List<ItemFile>> Categorize(IEnumerable<ItemFile> files)
    {
        var result = new Dictionary<ItemFileCategoryType, List<ItemFile>>();
        foreach (var file in files)
        {
            var category = ResolveCategory(file);
            if (!result.TryGetValue(category, out var list))
            {
                list = [];
                result[category] = list;
            }
            list.Add(file);
        }
        return result;
    }

    /// <summary>
    /// 指定したファイルのカテゴリ種別（<see cref="ItemFileCategoryType"/>）を判定します。
    /// </summary>
    /// <param name="file">判定対象のファイル。</param>
    /// <returns>判定されたカテゴリ種別。</returns>
    public static ItemFileCategoryType Categorize(ItemFile file) => ResolveCategory(file);

    private static ItemFileCategoryType ResolveCategory(ItemFile file)
    {
        var extension = Path.GetExtension(file.FilePath);
        if (string.IsNullOrEmpty(extension)) return ItemFileCategoryType.Unknown;

        var fileName = file.FileName;

        foreach (var rule in Rules)
        {
            if (rule.Extensions?.Contains(extension) is not true) continue;
            if (rule.FileNames == null) return rule.Category;

            if (rule.FileNames.Any(n => fileName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                return rule.Category;
        }
        return ItemFileCategoryType.Unknown;
    }
}
