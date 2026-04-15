using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemFileSearchService
{
    internal static ImmutableArray<ItemFile> ExecuteSearch(string itemPath, SearchFilter searchFilter)
    {
        if (!Directory.Exists(itemPath)) return ImmutableArray<ItemFile>.Empty;

        List<ItemFile> matchedFiles = new();
        foreach (string file in FileSystemService.EnumerateFiles(itemPath))
        {
            string fileName = Path.GetFileName(file);
            if (Matches(fileName, searchFilter))
            {
                matchedFiles.Add(new ItemFile(file));
            }
        }

        return matchedFiles
            .OrderByDescending(f => SearchUtils.GetScore(f.FileName, searchFilter.SearchTokens.Where(t => t.Type == SearchTokenType.FreeWord).Select(t => t.Value)))
            .ToImmutableArray();
    }

    private static bool Matches(string fileName, SearchFilter searchFilter)
    {
        foreach (SearchToken token in searchFilter.SearchTokens.Where(t => t.Type == SearchTokenType.FreeWord))
        {
            bool contains = fileName.Contains(token.Value, StringComparison.OrdinalIgnoreCase);
            if (token.IsNegation && contains) return false;
            if (!token.IsNegation && !contains) return false;
        }

        return true;
    }
}
