using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class CommonAvatarDatabaseManager : IDatabaseManager<CommonAvatar>
{
    private List<CommonAvatar> _commonAvatars { get; set; } = new();
    public IReadOnlyList<CommonAvatar> Items => _commonAvatars;

    public string DatabaseFilePath { get; } = SystemPath.CommonAvatarDatabasePath;
    public void Add(CommonAvatar commonAvatar) => _commonAvatars.Add(commonAvatar);
    public void AddRange(IEnumerable<CommonAvatar> commonAvatars) => _commonAvatars.AddRange(commonAvatars);
    public bool Remove(string id) => _commonAvatars.RemoveAll(i => i.Id == id) > 0;
    public void Update(IEnumerable<CommonAvatar> commonAvatars) => _commonAvatars = commonAvatars.ToList();
    public void Clear() => _commonAvatars.Clear();
}
