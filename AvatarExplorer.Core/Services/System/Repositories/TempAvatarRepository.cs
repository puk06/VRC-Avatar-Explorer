using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository
{
    private readonly DatabaseManager<TempAvatar> _db = new(SystemPath.TempAvatarsDatabasePath);

    public void Load(string? path = null) => _db.Load(path);
    public IReadOnlyList<TempAvatar> GetAll() => _db.Items;

    public TempAvatar? Get(string identifier) => _db.Items.FirstOrDefault(i => i.Identifier == identifier);

    public void Create(string avatarName)
    {
        _db.Add(new(avatarName));
    }

    public void Save() => _db.Save();
    public void Remove(string identifier)
    {
        var item = Get(identifier);
        if (item == null) return;

        _db.Remove(item.Id);
    }
}
