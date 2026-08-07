namespace AvatarExplorer.Core.Interfaces;

public interface IRepository<T> where T : class, IIdentifiable
{
    event Action? OnUpdated;
    IReadOnlyList<T> GetAll();
    T? Get(string identifier);
    void Load();
    void Remove(string identifier);
    void Save();
    void Clear();
    void MarkAsChanged();
}
