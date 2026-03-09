namespace AvatarExplorer.Core.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class FileNamesFilterAttribute(string filter) : Attribute
{
    public string Filter { get; } = filter;
}
