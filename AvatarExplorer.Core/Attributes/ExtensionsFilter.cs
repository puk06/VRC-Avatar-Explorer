namespace AvatarExplorer.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ExtensionsFilterAttribute(string filter) : Attribute
{
    public string Filter { get; } = filter;
}
