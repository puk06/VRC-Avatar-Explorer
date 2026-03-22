namespace AvatarExplorer.Core.Utils;

internal static class SearchUtils
{
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
