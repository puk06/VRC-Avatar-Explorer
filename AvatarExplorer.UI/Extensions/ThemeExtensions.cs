using Avalonia.Styling;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Attributes;
using AvatarExplorer.UI.Models.Common;

namespace AvatarExplorer.UI.Extensions;

public static class ThemeExtensions
{
    public static ThemeVariant GetThemeVariant(this Theme theme)
    {
        var variantName = theme.GetAttribute<ThemeVariantAttribute>()?.VariantName;
        
        return variantName switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            "Sakura" => AppThemeVariants.Sakura,
            "Mint" => AppThemeVariants.Mint,
            "Lavender" => AppThemeVariants.Lavender,
            "Ocean" => AppThemeVariants.Ocean,
            "Sunset" => AppThemeVariants.Sunset,
            "Forest" => AppThemeVariants.Forest,
            "Mocha" => AppThemeVariants.Mocha,
            "Slate" => AppThemeVariants.Slate,
            _ => ThemeVariant.Default
        };
    }
}
