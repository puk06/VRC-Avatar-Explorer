using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class CommonAvatarRepository
{
    private readonly DatabaseManager<CommonAvatar> _db = new(SystemPath.CommonAvatarDatabasePath);
    public void Load(string? path = null) => _db.Load(path);

    public IReadOnlyList<CommonAvatar> GetAll() => _db.Items;
    public CommonAvatar? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Remove(string identifier)
    {
        var group = Get(identifier);
        if (group == null) return;

        _db.Remove(group.Id);
    }

    public void Create(string groupName) => _db.Add(new(groupName));

    public void UpdateAvatars(string groupId, IEnumerable<string> avatars)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateAvatars(avatars);
        Save();
    }
    
    public void RenameGroup(string groupId, string newName)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.GroupName = newName;
        Save();
    }

    public void Save() => _db.Save();
}
