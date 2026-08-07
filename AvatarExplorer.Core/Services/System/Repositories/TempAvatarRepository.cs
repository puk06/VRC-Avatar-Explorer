using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository : RepositoryBase<TempAvatar>
{
    public TempAvatarRepository() : base(SystemPath.TempAvatarsDatabasePath) { }

    public override void Load()
    {
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
