namespace AvatarExplorer.Core.Utils;

internal static class SearchUtils
{
    internal static bool MatchesFilter<T>(IEnumerable<T> targets, IEnumerable<T> filters, bool isOrSearch, Func<T, T, bool> comparer)
    {
        if (!filters.Any()) return true;

        if (isOrSearch) return filters.Any(filter => targets.Any(target => comparer(target, filter)));
        else return filters.All(filter => targets.Any(target => comparer(target, filter)));
    }

    internal static bool GetWordSearchResult(string searchIndex, string word) => searchIndex.Contains(word, StringComparison.CurrentCultureIgnoreCase);

    internal static int GetScore(string searchIndex, IEnumerable<string> words)
    {
        int count = 0;

        foreach (string word in words)
        {
            int index = 0;

            while ((index = searchIndex.IndexOf(word, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += word.Length;
            }
        }

        return count;
    }
}
