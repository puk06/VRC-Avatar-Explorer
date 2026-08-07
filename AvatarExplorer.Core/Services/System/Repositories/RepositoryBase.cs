using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public abstract class RepositoryBase<T>(string dbPath) : IRepository<T> where T : class, IIdentifiable, IDatabaseItem
{
    protected readonly DatabaseManager<T> Db = new(dbPath);
    public event Action? OnUpdated;

    public IReadOnlyList<T> GetAll() => Db.Items;

    public T? Get(string identifier) => Db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public virtual void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        Db.Remove(item.Id);
        Db.Save();
        OnUpdated?.Invoke();
    }

    public void Save() => Db.Save();

    public void Clear()
    {
        Db.Clear();
        Db.Save();
        OnUpdated?.Invoke();
    }

    public void MarkAsChanged() => OnUpdated?.Invoke();

    public abstract void Load();

    protected void InvokeUpdated() => OnUpdated?.Invoke();

    public void Add(T item)
    {
        Db.Add(item);
        Db.Save();
        OnUpdated?.Invoke();
    }
}
