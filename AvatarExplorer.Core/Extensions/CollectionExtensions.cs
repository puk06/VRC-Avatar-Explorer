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

    public static bool IsValidIndex<T>(this IEnumerable<T> collection, int index)
    {
        if (collection is IReadOnlyCollection<T> list)
        {
            return index >= 0 && index < list.Count;
        }
        else if (collection is T[] array)
        {
            return index >= 0 && index < array.Length;
        }
        else
        {
            return index >= 0 && index < collection.Count();
        }
    }
}
