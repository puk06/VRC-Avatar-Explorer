using Avalonia.Media;

namespace AvatarExplorer.UI.Utils;

internal static class FontUtils
{
    private const string DefaultFontFamily = "Noto Sans JP";
    private const string AvaloniaFontFamilyPrefix = "avares://AvatarExplorer/Assets/Fonts#";

    internal static FontFamily GetFontFamily(string? fontFamilyName = null)
    {
        if (string.IsNullOrEmpty(fontFamilyName)) return new(AvaloniaFontFamilyPrefix + DefaultFontFamily);

        if (fontFamilyName.StartsWith(AvaloniaFontFamilyPrefix))
            return new(fontFamilyName);

        return new(AvaloniaFontFamilyPrefix + fontFamilyName);
    }
}
