using Avalonia.Media;

namespace AvatarExplorer.UI.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class ThemeVariantAttribute(string variantName, byte r, byte g, byte b) : Attribute
{
    internal string VariantName { get; } = variantName;
    internal Color BackgroundColor { get; } = new Color(255, r, g, b);
}
