using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Models.Items;

public class SearchContext
{
    public required IEnumerable<Item> Items { get; init; }
    public required IEnumerable<CommonAvatar> CommonAvatars { get; init; }
    public required IEnumerable<TempAvatar> TempAvatars { get; init; }
    public required Dictionary<string, ItemSearchIndex> ItemSearchIndices { get; init; }
    public required Dictionary<string, CommonAvatarSearchIndex> CommonAvatarSearchIndices { get; init; }
    public required Dictionary<string, TempAvatarSearchIndex> TempAvatarSearchIndices { get; init; }
    public required RuntimeSettings RuntimeSettings { get; init; }
}
