namespace AvatarExplorer.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class SearchFilterKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
