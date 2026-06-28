using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Items.Internal;

namespace AvatarExplorer.Core.Services.Avatars.Internal;

internal static class AvatarStatusResolver
{
    internal static AvatarStatus Resolve(Item item, string? avatarId, IEnumerable<CommonAvatar> commonAvatars, bool treatEmptySupportedAvatarAsNone = false)
    {
        var result = new AvatarStatus();
        if (string.IsNullOrEmpty(avatarId)) return result;

        if ((!treatEmptySupportedAvatarAsNone && !item.SupportedAvatars.Any()) || item.SupportedAvatars.Contains(avatarId))
            result.IsSupported = true;

        if (item.Type != ItemType.Clothing) return result;

        // アイテムの対応アバターが共通素体グループで登録されていた時用の処理
        foreach (var avatar in item.SupportedAvatars)
        {
            if (!avatar.StartsWith(CommonAvatar.InternalPathPrefix)) continue;

            var group = commonAvatars.FirstOrDefault(g => g.Id == CommonAvatar.GetGroupId(avatar));
            if (group != null && group.Avatars.Contains(avatarId))
            {
                result.IsCommon = true;
                result.CommonAvatarName = group.GroupName;
                return result;
            }
        }

        var groupsForPath = commonAvatars.Where(x => x.Avatars.Contains(avatarId));
        if (!groupsForPath.Any()) return result;

        foreach (var supportedAvatar in item.SupportedAvatars)
        {
            var group = groupsForPath.FirstOrDefault(g => g.Avatars.Contains(supportedAvatar));
            if (group != null)
            {
                result.IsCommon = true;
                result.CommonAvatarName = group.GroupName;
                return result;
            }
        }

        return result;
    }
}
