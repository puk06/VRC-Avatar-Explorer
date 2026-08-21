namespace AvatarExplorer.UI.ViewModels.Component;

public class ThumbnailSource
{
    public string Primary { get; set; } = string.Empty;
    public string? Fallback { get; set; }
    public string? FilePath { get; set; }
    public string Applied { get; set; } = string.Empty;
}
