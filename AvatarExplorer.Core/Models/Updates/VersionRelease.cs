namespace AvatarExplorer.Core.Models.Updates;

public class VersionRelease
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string? ReleaseUrl { get; set; }
    public ChangeLog ChangeLogs { get; set; } = new();
}
