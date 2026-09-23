using Avalonia.Media;

namespace AvatarExplorer.UI.Utils;

internal static class FontUtils
{
    private const string AvaloniaFontFamilyPrefix = "avares://AvatarExplorer/Assets/Fonts#";

    private static readonly string DefaultFontFamily = ToInternalFontFamilyName("Noto Sans JP");
    private static readonly string FallbackFontFamily = ToInternalFontFamilyName("Noto Sans");

    internal static FontFamily GetFontFamily(string? fontFamilyName = null)
    {
        // FontFamilyName, DefaultFontFamily, FallbackFontFamily の順
        string[] families = string.IsNullOrEmpty(fontFamilyName)
            ? [DefaultFontFamily, FallbackFontFamily]
            : [ToInternalFontFamilyName(fontFamilyName), DefaultFontFamily, FallbackFontFamily];

        return new(string.Join(", ", families));
    }

    private static string ToInternalFontFamilyName(string familyName)
        => familyName.StartsWith(AvaloniaFontFamilyPrefix, StringComparison.Ordinal)
            ? familyName
            : AvaloniaFontFamilyPrefix + familyName;
}
