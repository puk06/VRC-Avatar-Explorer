using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Attributes;

namespace AvatarExplorer.UI.Models.Common;

public enum BitmapAntiAliasingMode
{
    [BitmapInterpolation(BitmapInterpolationMode.None)]
    None,

    [BitmapInterpolation(BitmapInterpolationMode.LowQuality)]
    Low,

    [BitmapInterpolation(BitmapInterpolationMode.MediumQuality)]
    Medium,

    [BitmapInterpolation(BitmapInterpolationMode.HighQuality)]
    High
}
