using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class CommonAvatar : AbstractDatabaseItem, ISelectableItem
{
    public string GroupName { get; set; } = string.Empty;
    [JsonInclude] private List<string> Avatars { get; set; } = new List<string>();

    [JsonIgnore] public ImmutableArray<string> AvatarsView => Avatars.ToImmutableArray();

    public void UpdateAvatars(IEnumerable<string> avatars) => Avatars = avatars.ToList();

    [JsonIgnore] public static readonly string InternalPathPrefix = "<sys:commonavatar>";
    public string GetInternalId() => InternalPathPrefix + Id;
    public static string? GetGroupId(string internalId)
    {
        if (string.IsNullOrEmpty(internalId) || !internalId.StartsWith(InternalPathPrefix)) return null;
        return internalId[InternalPathPrefix.Length..];
    }
}
