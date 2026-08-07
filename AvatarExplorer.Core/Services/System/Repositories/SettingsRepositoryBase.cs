using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Services.System.Repositories;

public abstract class SettingsRepositoryBase<T> : ISettingsRepository<T> where T : class, new()
{
    protected readonly SettingsManager<T> Manager;
    public T Settings => Manager.Settings;
    public event Action<T>? OnSettingsChanged;

    protected SettingsRepositoryBase(string filePath)
    {
        Manager = new(filePath);
        Manager.SettingsChanged += settings => OnSettingsChanged?.Invoke(settings);
    }

    public abstract void Load(string? path = null);

    public void Update(T settings)
    {
        Manager.Update(settings);
        Manager.Save();
    }

    public void Save() => Manager.Save();

    protected void InvokeSettingsChanged(T settings) => OnSettingsChanged?.Invoke(settings);
}
