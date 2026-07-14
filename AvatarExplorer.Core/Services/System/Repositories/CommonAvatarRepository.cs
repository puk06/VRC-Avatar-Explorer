using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Database;

namespace AvatarExplorer.Core.Services.System.Repositories;

public class CommonAvatarRepository
{
    private readonly DatabaseManager<CommonAvatar> _db = new(SystemPath.CommonAvatarDatabasePath);

    public void Load(string? path = null) => _db.Load(path);
    public IReadOnlyList<CommonAvatar> GetAll() => _db.Items;
    public CommonAvatar? GetById(string id) => _db.GetById(id);
    public void Add(CommonAvatar commonAvatar) => _db.Add(commonAvatar);
    public void Save() => _db.Save();
    public void Remove(string id) => _db.Remove(id);
}
