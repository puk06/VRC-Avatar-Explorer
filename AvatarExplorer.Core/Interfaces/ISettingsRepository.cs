namespace AvatarExplorer.Core.Interfaces;

public interface ISettingsRepository<T> where T : class, new()
{
    T Settings { get; }
    event Action<T>? OnSettingsChanged;
    void Load(string? path = null);
    void Update(T settings);
    void Save();
}
