using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

public static class AvatarService
{
    public static ImmutableArray<string> GetAllSupportedAvatarIds(IEnumerable<string> avatars, IEnumerable<CommonAvatar> commonAvatars, bool includeCommonAvatarToSupported = false)
    {
        var avatarIds = new HashSet<string>();

        foreach (var id in avatars)
        {
            if (id.StartsWith("commonavatar:"))
            {
                var commonAvatar = commonAvatars.FirstOrDefault(i => i.Identifier == id);
                if (commonAvatar != null) avatarIds.UnionWith(commonAvatar.Avatars);

                continue;
            }

            avatarIds.Add(id);

            if (!includeCommonAvatarToSupported) continue;

            var commonAvatarGroup = commonAvatars.Where(commonAvatar => commonAvatar.Avatars.Contains(id));
            foreach (var commonAvatarId in commonAvatarGroup.SelectMany(i => i.Avatars))
            {
                avatarIds.Add(commonAvatarId);
            }
        }

        return avatarIds.ToImmutableArray();
    }
}
