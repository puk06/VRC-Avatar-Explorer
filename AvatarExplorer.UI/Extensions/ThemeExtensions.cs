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
        if (variant == null) return (ThemeVariant.Default, Colors.Transparent);

        var variantName = variant.VariantName;
        var backgroundColor = variant.BackgroundColor;

        return variantName switch
        {
            nameof(AppThemeVariants.Dark) => (AppThemeVariants.Dark, backgroundColor),
            nameof(AppThemeVariants.Light) => (AppThemeVariants.Light, backgroundColor),
            nameof(AppThemeVariants.Sakura) => (AppThemeVariants.Sakura, backgroundColor),
            nameof(AppThemeVariants.Mint) => (AppThemeVariants.Mint, backgroundColor),
            nameof(AppThemeVariants.Lavender) => (AppThemeVariants.Lavender, backgroundColor),
            nameof(AppThemeVariants.Ocean) => (AppThemeVariants.Ocean, backgroundColor),
            nameof(AppThemeVariants.Sunset) => (AppThemeVariants.Sunset, backgroundColor),
            nameof(AppThemeVariants.Forest) => (AppThemeVariants.Forest, backgroundColor),
            nameof(AppThemeVariants.Mocha) => (AppThemeVariants.Mocha, backgroundColor),
            nameof(AppThemeVariants.Slate) => (AppThemeVariants.Slate, backgroundColor),
            _ => (ThemeVariant.Default, Colors.Transparent)
        };
    }
}
