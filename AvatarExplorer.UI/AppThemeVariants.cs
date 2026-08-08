using Avalonia.Styling;

namespace AvatarExplorer.UI;

public static class AppThemeVariants
{
    public static readonly ThemeVariant Dark = ThemeVariant.Dark;
    public static readonly ThemeVariant Light = ThemeVariant.Light;
    public static readonly ThemeVariant Sakura = new(nameof(Sakura), ThemeVariant.Light);
    public static readonly ThemeVariant Mint = new(nameof(Mint), ThemeVariant.Light);
    public static readonly ThemeVariant Lavender = new(nameof(Lavender), ThemeVariant.Light);
    public static readonly ThemeVariant Ocean = new(nameof(Ocean), ThemeVariant.Dark);
    public static readonly ThemeVariant Sunset = new(nameof(Sunset), ThemeVariant.Light);
    public static readonly ThemeVariant Forest = new(nameof(Forest), ThemeVariant.Light);
    public static readonly ThemeVariant Mocha = new(nameof(Mocha), ThemeVariant.Dark);
    public static readonly ThemeVariant Slate = new(nameof(Slate), ThemeVariant.Dark);
}
