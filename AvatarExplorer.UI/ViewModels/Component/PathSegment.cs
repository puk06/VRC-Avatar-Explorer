namespace AvatarExplorer.UI.ViewModels.Component;

public class PathSegment
{
    public string DisplayName { get; init; } = string.Empty;
    public string? State { get; init; }
    public bool IsClickable => State != null;
}
