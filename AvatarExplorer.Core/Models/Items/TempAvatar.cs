using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class TempAvatar(string avatarName) : AbstractDatabaseItem, ISelectableItem
{
    public string AvatarName { get; set; } = avatarName;

    [JsonIgnore] public static readonly string InternalPathPrefix = "<sys:temp>";
    public string GetInternalId() => InternalPathPrefix + Id;
    public static string? GetAvatarId(string internalId)
    {
        if (string.IsNullOrEmpty(internalId) || !internalId.StartsWith(InternalPathPrefix)) return null;
        return internalId[InternalPathPrefix.Length..];
    }
}
