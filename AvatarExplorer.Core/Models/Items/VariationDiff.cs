namespace AvatarExplorer.Core.Models.Items;

public record VariationDiff(
    IReadOnlyList<DownloadableFile> Added,
    IReadOnlyList<DownloadableFile> Removed,
    IReadOnlyList<DownloadableFile> Changed
)
{
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
}
