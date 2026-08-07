using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AvatarExplorer.UI.Interfaces;

public static class IInitializableRegistry
{
    private static readonly List<(int, IInitializable)> _instances = [];
    private static readonly List<(int, IPostInitializable)> _postInstances = [];
    private static bool _completed;

    public static void Register(int priority, IInitializable instance)
    {
        _instances.Add((priority, instance));
    }

    public static void Register(int priority, IPostInitializable instance)
    {
        _postInstances.Add((priority, instance));
    }

    public static async Task Complete()
    {
        if (_completed) return;
        _completed = true;

        foreach (var instance in _instances.OrderBy(i => i.Item1))
        {
            await instance.Item2.Initialize();
        }

        foreach (var instance in _postInstances.OrderBy(i => i.Item1))
        {
            await instance.Item2.OnInitialized();
        }
    }
}
