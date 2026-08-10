using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository : RepositoryBase<TempAvatar>
{
    public TempAvatarRepository() : base(SystemPath.TempAvatarsDatabasePath) { }

    public override void Load()
    {
        DatabaseMigrationService.MigrateDatabase(
            Db.DatabaseFilePath,
            0,
            (_, _) => false);

        Db.Load();
        InvokeUpdated();
    }

    public void Create(string avatarName)
    {
        Add(new(avatarName));
    }

    public void RenameAvatar(string identifier, string newName)
    {
        var avatar = Get(identifier);
        if (avatar == null) return;

        avatar.UpdateAvatarName(newName);
        Save();
        InvokeUpdated();
    }
}
