using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class TempAvatarsDatabaseManager : AbstractDatabaseManager<TempAvatar>
{
    public override string DatabaseFilePath { get; } = SystemPath.TempAvatarsDatabasePath;
}
