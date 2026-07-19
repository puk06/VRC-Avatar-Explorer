using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class TempAvatar(string avatarName) : AbstractDatabaseItem, INavigationable
{
    public string AvatarName { get; set; } = avatarName;

    [JsonIgnore] public string Identifier => "tempavatar:" + Id;
}
