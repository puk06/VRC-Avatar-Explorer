namespace AvatarExplorer.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class LocalizationKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
