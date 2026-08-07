using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class CommonAvatarRepository : RepositoryBase<CommonAvatar>
{
    public CommonAvatarRepository() : base(SystemPath.CommonAvatarDatabasePath) { }

    public override void Load()
    {
        DatabaseMigrationService.Migrate(
            Db.DatabaseFilePath,
            DatabaseMigrations.CommonAvatarVersion,
            DatabaseMigrations.ApplyCommonAvatarMigration);

        Db.Load();
        InvokeUpdated();
    }

    public void Create(string groupName)
    {
        Add(new(groupName));
    }

    public void UpdateAvatars(string groupId, IEnumerable<string> avatars)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateAvatars(avatars);
        Save();
        InvokeUpdated();
    }

    public void RenameGroup(string groupId, string newName)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateGroupName(newName);
        Save();
        InvokeUpdated();
    }
}
