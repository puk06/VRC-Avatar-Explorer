using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class CommonAvatar : AbstractDatabaseItem, ISelectableItem
{
    public string GroupName { get; set; } = string.Empty;
    [JsonInclude] public ImmutableArray<string> Avatars { get; private set; } = [];

    public void UpdateAvatars(IEnumerable<string> avatars) => Avatars = avatars.ToImmutableArray();

    [JsonIgnore] public string Identifier => "commonavatar:" + Id;
}
