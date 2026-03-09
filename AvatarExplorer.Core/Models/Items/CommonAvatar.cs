using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.Database;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class CommonAvatar : ISelectableItem, IDatabaseItem
{
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string GroupName { get; set; } = string.Empty;
    [JsonInclude] private List<string> Avatars { get; set; } = new List<string>();

    [JsonIgnore] public IReadOnlyList<string> AvatarsView => Avatars;

    public void UpdateAvatars(IEnumerable<string> avatars) => Avatars = avatars.ToList();

    [JsonIgnore] public static readonly string InternalPathPrefix = "<sys:commonavatar>";
    public string GetInternalId() => InternalPathPrefix + Id;
    public static string? GetGroupId(string internalId)
    {
        if (string.IsNullOrEmpty(internalId) || !internalId.StartsWith(InternalPathPrefix)) return null;
        return internalId[InternalPathPrefix.Length..];
    }
}
