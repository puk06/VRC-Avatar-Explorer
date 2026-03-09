using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public record ItemCountInfo(ISelectableItem Item, int Count, string[]? Args = null);
