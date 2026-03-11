using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class CommonAvatarDatabaseManager : AbstractDatabaseManager<CommonAvatar>
{
    public override string DatabaseFilePath { get; } = SystemPath.CommonAvatarDatabasePath;
}
