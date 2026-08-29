namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// コレクションに対する汎用的な拡張メソッドを提供します。
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// コレクションの各要素に対して指定したアクションを実行します。リストや配列の場合はインデックス順に高速に処理されます。
    /// </summary>
    /// <typeparam name="T">要素の型。</typeparam>
    /// <param name="collection">対象のコレクション。</param>
    /// <param name="action">各要素に適用するアクション。</param>
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

    /// <summary>
    /// 指定したインデックスがコレクションの範囲内（0 以上、要素数未満）かどうかを判定します。
    /// </summary>
    /// <typeparam name="T">要素の型。</typeparam>
    /// <param name="collection">対象のコレクション。</param>
    /// <param name="index">判定するインデックス。</param>
    /// <returns>インデックスが有効な範囲内の場合は true。</returns>
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
