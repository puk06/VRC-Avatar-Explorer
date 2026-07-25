using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Sort;

namespace AvatarExplorer.UI.Models.Settings;

public record UserPreferences
{
    public int Language { get; init; } = 0;
    public int NormalIconSize { get; init; } = 70;
    public bool EnableHoverIconSize { get; init; } = true;
    public int HoverIconSize { get; init; } = 200;
    public int ThumbnailCompressionMaxEdge { get; init; } = 256;
    public bool UseBackgroundImage { get; init; } = false;
    public string BackgroundImage { get; init; } = string.Empty;
    public int BackgroundOpacity { get; init; } = 20;
    public Theme Theme { get; init; } = Theme.Dark;
    public int ItemsPerPage { get; init; } = 30;
    public BitmapAntiAliasingMode AntiAliasingMode { get; init; } = BitmapAntiAliasingMode.None;
    public bool RemoveBrackets { get; init; } = false;
    public ItemSortOrder SortOrder { get; init; } = ItemSortOrder.UpdatedDate;
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;
}
