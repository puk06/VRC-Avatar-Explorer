using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

public static class AvatarStatusResolver
{
    public static AvatarStatus Resolve(Item item, string? avatarId, IEnumerable<CommonAvatar> commonAvatars, bool treatEmptySupportedAvatarAsNone = false)
    {
        var result = new AvatarStatus();
        if (string.IsNullOrEmpty(avatarId)) return result;

        if ((!treatEmptySupportedAvatarAsNone && !item.SupportedAvatars.Any()) || item.SupportedAvatars.Contains(avatarId))
            result.IsSupported = true;

        // アイテムの対応アバターが共通素体グループで登録されていた時用の処理
        foreach (var id in item.SupportedAvatars)
        {
            if (!id.StartsWith("commonavatar:")) continue;

            var group = commonAvatars.FirstOrDefault(g => g.Identifier == id);
            if (group?.Avatars.Contains(avatarId) is true)
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
