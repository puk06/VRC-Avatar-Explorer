using System.Collections.Immutable;

namespace AvatarExplorer.Core.Interfaces.Database;

internal interface IDatabaseManager<T>
    where T : IDatabaseItem
{
    string DatabaseFilePath { get; }
    ImmutableArray<T> Items { get; }
    void Add(T item);
    void AddRange(IEnumerable<T> items);
    bool Remove(string id);
    void Update(IEnumerable<T> items);
    void Clear();
}
