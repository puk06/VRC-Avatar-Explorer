using Material.Icons;

namespace AvatarExplorer.UI.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class MaterialIconAttribute(MaterialIconKind materialIconKind) : Attribute
{
    internal MaterialIconKind MaterialIconKind { get; } = materialIconKind;
}
