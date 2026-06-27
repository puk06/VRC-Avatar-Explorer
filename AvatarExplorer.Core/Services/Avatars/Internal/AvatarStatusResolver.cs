using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Items.Internal;

namespace AvatarExplorer.Core.Services.Avatars.Internal;

internal static class AvatarStatusResolver
{
    internal static AvatarStatus Resolve(Item item, string? avatarId, IEnumerable<CommonAvatar> commonAvatars, bool treatEmptySupportedAvatarAsNone = false)
    {
        AvatarStatus avatarStatus = new();
        if (string.IsNullOrEmpty(avatarId)) return avatarStatus;

        if ((!treatEmptySupportedAvatarAsNone && !item.SupportedAvatars.Any()) || item.SupportedAvatars.Contains(avatarId))
            avatarStatus.IsSupported = true;

        if (item.Type != ItemType.Clothing) return avatarStatus;

        // アイテムの対応アバターが共通素体グループで登録されていた時用の処理
        foreach (string supportedAvatar in item.SupportedAvatars)
        {
            if (!supportedAvatar.StartsWith(CommonAvatar.InternalPathPrefix)) continue;

            CommonAvatar? group = commonAvatars.FirstOrDefault(g => g.Id == CommonAvatar.GetGroupId(supportedAvatar));
            if (group != null && group.Avatars.Contains(avatarId))
            {
                avatarStatus.IsCommon = true;
                avatarStatus.CommonAvatarName = group.GroupName;
                return avatarStatus;
            }
        }

        IEnumerable<CommonAvatar> groupsForPath = commonAvatars
            .Where(x => x.Avatars.Contains(avatarId));

        if (!groupsForPath.Any()) return avatarStatus;

        foreach (string supportedAvatar in item.SupportedAvatars)
        {
            CommonAvatar? group = groupsForPath.FirstOrDefault(g => g.Avatars.Contains(supportedAvatar));
            if (group != null)
            {
                avatarStatus.IsCommon = true;
                avatarStatus.CommonAvatarName = group.GroupName;
                return avatarStatus;
            }
        }

        return avatarStatus;
    }
}
