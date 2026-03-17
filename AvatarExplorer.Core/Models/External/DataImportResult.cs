using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External;

public class DataImportResult
{
    public List<Item> Items { get; } = new();
    public List<CommonAvatar> CommonAvatars { get; } = new();
    public List<TempAvatar> TempAvatars { get; } = new();
}
