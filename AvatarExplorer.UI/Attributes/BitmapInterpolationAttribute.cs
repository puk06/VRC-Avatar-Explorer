using Avalonia.Media.Imaging;

namespace AvatarExplorer.UI.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class BitmapInterpolationAttribute(BitmapInterpolationMode bitmapInterpolationMode) : Attribute
{
    internal BitmapInterpolationMode BitmapInterpolationMode { get; } = bitmapInterpolationMode;
}
