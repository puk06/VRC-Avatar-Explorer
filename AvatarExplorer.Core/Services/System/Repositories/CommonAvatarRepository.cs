using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class CommonAvatarRepository
{
    private readonly DatabaseManager<CommonAvatar> _db = new(SystemPath.CommonAvatarDatabasePath);

    /// <summary>
    /// CommonAvatar が追加・更新・削除された際に発火します。引数は CommonAvatar.Identifier です。
    /// </summary>
    public event Action<string>? OnUpdated;

    public void Load(string? path = null) => _db.Load(path);

    public IReadOnlyList<CommonAvatar> GetAll() => _db.Items;
    public CommonAvatar? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Remove(string identifier)
    {
        var group = Get(identifier);
        if (group == null) return;

        _db.Remove(group.Id);
        OnUpdated?.Invoke(identifier);
    }

    public void Create(string groupName)
    {
        var group = new CommonAvatar(groupName);
        _db.Add(group);
        OnUpdated?.Invoke(group.Identifier);
    }

    public void UpdateAvatars(string groupId, IEnumerable<string> avatars)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateAvatars(avatars);
        Save();
        OnUpdated?.Invoke(groupId);
    }
    
    public void RenameGroup(string groupId, string newName)
    {
        var group = Get(groupId);
        if (group == null) return;

        group.UpdateGroupName(newName);
        Save();
        OnUpdated?.Invoke(groupId);
    }

    public void Save() => _db.Save();
}
