using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class TempAvatar(string avatarName) : AbstractDatabaseItem, ISelectableItem
{
    public string AvatarName { get; set; } = avatarName;

    [JsonIgnore] public string Identifier => "tempavatar:" + Id;
}
