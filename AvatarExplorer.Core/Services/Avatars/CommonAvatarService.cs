using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

internal static class CommonAvatarService
{
    internal static CommonAvatar? GetCommonAvatarFromName(IEnumerable<CommonAvatar> commonAvatars, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return commonAvatars.FirstOrDefault(commonAvatar => commonAvatar.GroupName == name);
    }
}
