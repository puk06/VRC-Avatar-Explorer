using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class TempAvatar(string avatarName) : AbstractDatabaseItem, INavigationable
{
    [JsonInclude]  public string AvatarName { get; private set; } = avatarName;

    [JsonIgnore] public string Identifier => "tempavatar:" + Id;

    public void UpdateAvatarName(string newName) => AvatarName = newName;
}
