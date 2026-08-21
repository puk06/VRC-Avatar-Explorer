using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

public static class FileCategorizer
{
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
