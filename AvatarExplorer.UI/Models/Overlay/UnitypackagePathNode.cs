using System;
using System.Collections.Generic;

namespace AvatarExplorer.UI.Models.Overlay;

public sealed class UnitypackagePathNode(string name, string fullPath)
{
    public string Name { get; } = name;
    public string FullPath { get; } = NormalizePath(fullPath);
    public bool IsFile { get; private set; }
    public List<UnitypackagePathNode> Children { get; } = new();

    private readonly Dictionary<string, UnitypackagePathNode> _childMap = new(StringComparer.OrdinalIgnoreCase);

    public UnitypackagePathNode GetOrAddChild(string childName)
    {
        if (_childMap.TryGetValue(childName, out UnitypackagePathNode? existingNode)) return existingNode;

        UnitypackagePathNode createdNode = new(childName, $"{FullPath}/{childName}");
        _childMap[childName] = createdNode;
        Children.Add(createdNode);

        return createdNode;
    }

    public void MarkAsFile() => IsFile = true;
    
    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
