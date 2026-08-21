using System;
using System.Collections.Generic;

namespace AvatarExplorer.UI.ViewModels.Component;

public sealed class UnitypackagePathNodeViewModel(string name, string fullPath)
{
    public string Name { get; } = name;
    public string FullPath { get; } = NormalizePath(fullPath);
    public bool IsFile { get; private set; }
    public List<UnitypackagePathNodeViewModel> Children { get; } = [];

    private readonly Dictionary<string, UnitypackagePathNodeViewModel> _childMap = new(StringComparer.OrdinalIgnoreCase);

    public UnitypackagePathNodeViewModel GetOrAddChild(string childName)
    {
        if (_childMap.TryGetValue(childName, out UnitypackagePathNodeViewModel? existingNode)) return existingNode;

        var createdNode = new UnitypackagePathNodeViewModel(childName, $"{FullPath}/{childName}");
        _childMap[childName] = createdNode;
        Children.Add(createdNode);

        return createdNode;
    }

    public void MarkAsFile() => IsFile = true;

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
