using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class ItemDatabaseManager : AbstractDatabaseManager<Item>
{
    public override string DatabaseFilePath { get; } = SystemPath.ItemDatabasePath;
}
