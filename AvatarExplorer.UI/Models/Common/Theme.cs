using AvatarExplorer.UI.Attributes;

namespace AvatarExplorer.UI.Models.Common;

public enum Theme
{
    [ThemeVariant("Dark", 32, 32, 32)]
    Dark,
    
    [ThemeVariant("Light", 235, 235, 235)]
    Light,
    
    [ThemeVariant("Sakura", 233, 224, 228)]
    Sakura,
    
    [ThemeVariant("Mint", 219, 231, 225)]
    Mint,
    
    [ThemeVariant("Lavender", 223, 216, 236)]
    Lavender,
    
    [ThemeVariant("Ocean", 64, 88, 107)]
    Ocean,
    
    [ThemeVariant("Sunset", 240, 221, 208)]
    Sunset,
    
    [ThemeVariant("Forest", 214, 226, 213)]
    Forest,
    
    [ThemeVariant("Mocha", 110, 91, 85)]
    Mocha,
    
    [ThemeVariant("Slate", 87, 94, 110)]
    Slate
}
