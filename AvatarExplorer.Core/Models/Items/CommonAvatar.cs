using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class CommonAvatar(string groupName) : AbstractDatabaseItem, INavigationable
{
    [JsonInclude] public string GroupName { get; private set; } = groupName;
    [JsonInclude] public ImmutableArray<string> Avatars { get; private set; } = [];

    public void UpdateGroupName(string newName) => GroupName = newName;
    public void UpdateAvatars(IEnumerable<string> avatars) => Avatars = avatars.ToImmutableArray();

    [JsonIgnore] public string Identifier => "commonavatar:" + Id;
}
