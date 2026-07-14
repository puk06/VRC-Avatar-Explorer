using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class TempAvatarRepository
{
    private readonly DatabaseManager<TempAvatar> _db = new(SystemPath.TempAvatarsDatabasePath);

    public void Load(string? path = null) => _db.Load(path);
    public IReadOnlyList<TempAvatar> GetAll() => _db.Items;
    public TempAvatar? GetById(string id) => _db.GetById(id);
    public void Add(TempAvatar avatar) => _db.Add(avatar);
    public void Save() => _db.Save();
    public void Remove(string id) => _db.Remove(id);
}
