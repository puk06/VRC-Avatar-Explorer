using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Attributes;
using AvatarExplorer.UI.Models.Common;

namespace AvatarExplorer.UI.Extensions;

internal static class AntiAliasingModeExtensions
{
    internal static BitmapInterpolationMode GetInterpolationMode(this BitmapAntiAliasingMode bitmapAntiAliasingMode)
    {
        return bitmapAntiAliasingMode.GetAttribute<BitmapInterpolationAttribute>()?.BitmapInterpolationMode ?? BitmapInterpolationMode.Unspecified;
    }
}
