using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

public static class AvatarService
{
    public static ImmutableArray<string> GetAllSupportedAvatarIds(IEnumerable<string> avatars, IEnumerable<CommonAvatar> commonAvatars, bool includeCommonAvatarToSupported = false)
    {
        var avatarIds = new HashSet<string>();

        foreach (var avatarId in avatars)
        {
            if (avatarId.StartsWith(CommonAvatar.InternalPathPrefix))
            {
                var groupId = CommonAvatar.GetGroupId(avatarId);
                if (groupId == null) continue;

                var commonAvatar = commonAvatars.FirstOrDefault(i => i.Id == groupId);
                if (commonAvatar != null) avatarIds.UnionWith(commonAvatar.Avatars);

                continue;
            }

            avatarIds.Add(avatarId);

            if (!includeCommonAvatarToSupported) continue;

            var commonAvatarGroup = commonAvatars.Where(commonAvatar => commonAvatar.Avatars.Contains(avatarId));
            foreach (var commonAvatarId in commonAvatarGroup.SelectMany(i => i.Avatars))
            {
                avatarIds.Add(commonAvatarId);
            }
        }

        return avatarIds.ToImmutableArray();
    }
}
