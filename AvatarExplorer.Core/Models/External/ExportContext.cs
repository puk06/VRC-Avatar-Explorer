using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Models.External;

public class ExportContext
{
    public required IEnumerable<Item> Items { get; init; }
    public required IEnumerable<CommonAvatar> CommonAvatars { get; init; }
    public required IEnumerable<TempAvatar> TempAvatars { get; init; }
    public Func<ItemType, ValueTask<string?>>? ItemTypeLocalizer { get; init; }
    public required RuntimeSettings RuntimeSettings { get; init; }
}
