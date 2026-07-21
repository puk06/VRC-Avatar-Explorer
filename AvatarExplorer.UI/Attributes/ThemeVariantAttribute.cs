using System;

namespace AvatarExplorer.UI.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class ThemeVariantAttribute(string variantName) : Attribute
{
    internal string VariantName { get; } = variantName;
}
