using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository
{
    private readonly DatabaseManager<TempAvatar> _db = new(SystemPath.TempAvatarsDatabasePath);

    /// <summary>
    /// TempAvatar が追加・更新・削除された際に発火します。
    /// </summary>
    public event Action? OnUpdated;

    public void Load(string? path = null)
    {
        _db.Load(path);
        OnUpdated?.Invoke();
    }
    public IReadOnlyList<TempAvatar> GetAll() => _db.Items;

    public TempAvatar? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Create(string avatarName)
    {
        _db.Add(new(avatarName));
        Save();

        OnUpdated?.Invoke();
    }

    internal void Add(TempAvatar avatar) => _db.Add(avatar);

    public void Save() => _db.Save();
    public void Remove(string identifier)
    {
        var avatar = Get(identifier);
        if (avatar == null) return;

        _db.Remove(avatar.Id);
        Save();

        OnUpdated?.Invoke();
    }

    public void RenameAvatar(string identifier, string newName)
    {
        var avatar = Get(identifier);
        if (avatar == null) return;

        avatar.UpdateAvatarName(newName);
        Save();

        OnUpdated?.Invoke();
    }

    public void MarkAsChanged() => OnUpdated?.Invoke();
}
