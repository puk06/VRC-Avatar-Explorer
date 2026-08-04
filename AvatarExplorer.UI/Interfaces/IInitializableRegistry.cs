using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvatarExplorer.UI.Interfaces;

public static class IInitializableRegistry
{
    private static readonly List<IInitializable> _instances = [];
    private static readonly List<IPostInitializable> _postInstances = [];
    private static bool _completed;

    public static event Action? OnInitialized;

    public static void Register(IInitializable instance)
    {
        _instances.Add(instance);
    }

    public static void Register(IPostInitializable instance)
    {
        _postInstances.Add(instance);
    }

    public static async Task Complete()
    {
        if (_completed) return;
        _completed = true;

        foreach (var instance in _instances)
        {
            await instance.Initialize();
        }

        foreach (var instance in _postInstances)
        {
            await instance.OnInitialized();
        }

        OnInitialized?.Invoke();
    }
}
