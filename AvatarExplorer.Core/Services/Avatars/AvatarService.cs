using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

public static class AvatarService
{
    public static IReadOnlyList<string> GetAllSupportedAvatarIds(IEnumerable<string> avatars, IReadOnlyList<CommonAvatar> commonAvatars, bool includeCommonAvatarToSupported = false)
    {
        List<string> avatarIds = new();

        foreach (string avatarId in avatars)
        {
            if (avatarId.StartsWith(CommonAvatar.InternalPathPrefix))
            {
                string? groupId = CommonAvatar.GetGroupId(avatarId);
                if (groupId == null) continue;

                CommonAvatar? commonAvatar = commonAvatars.FirstOrDefault(i => i.Id == groupId);
                if (commonAvatar != null) avatarIds.AddRange(commonAvatar.AvatarsView);

                continue;
            }

            avatarIds.Add(avatarId);

            if (!includeCommonAvatarToSupported) continue;

            IEnumerable<CommonAvatar> commonAvatarGroup = commonAvatars.Where(commonAvatar => commonAvatar.AvatarsView.Contains(avatarId));
            foreach (string commonAvatarId in commonAvatarGroup.SelectMany(i => i.AvatarsView))
            {
                avatarIds.Add(commonAvatarId);
            }
        }

        return avatarIds.Distinct().ToList();
    }
}
