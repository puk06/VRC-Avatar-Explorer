namespace AvatarExplorer.Core.Extensions;

public static class SortExtensions
{
    public static IEnumerable<string> SortByFileName(this IEnumerable<string> source)
    {
        return source.OrderBy(path => Path.GetFileName(path) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);
    }
}
