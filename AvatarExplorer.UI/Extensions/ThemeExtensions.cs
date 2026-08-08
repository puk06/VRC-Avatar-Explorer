using Avalonia.Media;
using Avalonia.Styling;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Attributes;
using AvatarExplorer.UI.Models.Common;

namespace AvatarExplorer.UI.Extensions;

public static class ThemeExtensions
{
    public static (ThemeVariant Theme, Color BackgroundColor) GetThemeVariant(this Theme theme)
    {
        var variant = theme.GetAttribute<ThemeVariantAttribute>();
        var variantName = variant?.VariantName;
        var backgroundColor = variant?.BackgroundColor ?? Colors.Transparent;

        return variantName switch
        {
            "Dark" => (AppThemeVariants.Dark, backgroundColor),
            "Light" => (AppThemeVariants.Light, backgroundColor),
            "Sakura" => (AppThemeVariants.Sakura, backgroundColor),
            "Mint" => (AppThemeVariants.Mint, backgroundColor),
            "Lavender" => (AppThemeVariants.Lavender, backgroundColor),
            "Ocean" => (AppThemeVariants.Ocean, backgroundColor),
            "Sunset" => (AppThemeVariants.Sunset, backgroundColor),
            "Forest" => (AppThemeVariants.Forest, backgroundColor),
            "Mocha" => (AppThemeVariants.Mocha, backgroundColor),
            "Slate" => (AppThemeVariants.Slate, backgroundColor),
            _ => (ThemeVariant.Default, Colors.Transparent)
        };
    }
}
