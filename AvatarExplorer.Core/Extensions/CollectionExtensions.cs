namespace AvatarExplorer.Core.Extensions;

public static class CollectionExtensions
{
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        if (typeof(T) is IReadOnlyCollection<T> list)
        {
            for (int i = 0; i < list.Count; i++) action(list.ElementAt(i));
        }
        else if (typeof(T) is T[] array)
        {
            for (int i = 0; i < array.Length; i++) action(array[i]);
        }
        else
        {
            foreach (var value in collection) action(value);
        }
    }
}
