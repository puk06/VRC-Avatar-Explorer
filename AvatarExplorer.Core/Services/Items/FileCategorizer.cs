using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

/// <summary>
/// アイテム内のファイルを、拡張子などのルールに基づいて <see cref="ItemFileCategoryType"/> ごとに分類するためのユーティリティクラスです。
/// </summary>
public static class FileCategorizer
{
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

    private static ItemFileCategoryType ResolveCategory(ItemFile file)
    {
        var extension = Path.GetExtension(file.FilePath).ToLowerInvariant();
        var fileName = file.FileName.ToLowerInvariant();
        var candidates = Enum.GetValues<ItemFileCategoryType>()
            .Where(c => c != ItemFileCategoryType.None && c != ItemFileCategoryType.Unknown);

        foreach (var category in candidates)
        {
            var extensions = category.GetExtensionFilters();
            if (extensions?.Contains(extension) is true)
            {
                var fileNames = category.GetFileNameFilters();
                if (fileNames == null) return category;

                if (fileNames.Any(n => fileName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                    return category;
            }
        }
        return ItemFileCategoryType.Unknown;
    }
}
