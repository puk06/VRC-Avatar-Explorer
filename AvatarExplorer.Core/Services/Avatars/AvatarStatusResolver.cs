using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

/// <summary>
/// アイテムが指定したアバターに対応しているか（直接、あるいは共通素体グループを通じて）を判定するためのユーティリティクラスです。
/// </summary>
public static class AvatarStatusResolver
{
    /// <summary>
    /// アイテムが指定したアバターに対応しているかを判定します。
    /// 対応アバターにアバターIDが直接含まれる場合は <see cref="AvatarStatus.IsSupported"/> が、
    /// 共通素体グループを通じて間接的に対応する場合は <see cref="AvatarStatus.IsCommon"/> が <c>true</c> になります。
    /// <see cref="Item.SkipIndirectCommonAvatarCheck"/> が <c>true</c> の場合、間接的な共通素体グループ経由の判定のみがスキップされます。
    /// 対応アバターに共通素体グループが直接設定されている場合は、このフラグに関わらず判定されます。
    /// </summary>
    /// <param name="item">判定対象のアイテム。</param>
    /// <param name="avatarId">現在表示中のアバターID。空の場合は未対応として扱います。</param>
    /// <param name="commonAvatars">共通素体グループの一覧。</param>
    /// <param name="treatEmptySupportedAvatarAsNone">
    /// <c>true</c> の場合、対応アバターが空のときのみ未対応とみなします（空でも対応ありと扱うかどうかの切り替え）。
    /// </param>
    /// <returns>判定結果を保持する <see cref="AvatarStatus"/>。</returns>
    public static AvatarStatus Resolve(Item item, string? avatarId, IEnumerable<CommonAvatar> commonAvatars, bool treatEmptySupportedAvatarAsNone = false)
    {
        var result = new AvatarStatus();
        if (string.IsNullOrEmpty(avatarId)) return result;

        CheckDirectSupport(item, avatarId, treatEmptySupportedAvatarAsNone, result);
        CheckDirectCommonAvatarGroup(item, avatarId, commonAvatars, result);

        if (item.SkipIndirectCommonAvatarCheck) return result;

        CheckIndirectCommonAvatar(item, avatarId, commonAvatars, result);

        return result;
    }

    private static void CheckDirectSupport(Item item, string avatarId, bool treatEmptySupportedAvatarAsNone, AvatarStatus result)
    {
        if ((!treatEmptySupportedAvatarAsNone && !item.SupportedAvatars.Any()) || item.SupportedAvatars.Contains(avatarId))
            result.IsSupported = true;
    }

    private static void CheckDirectCommonAvatarGroup(Item item, string avatarId, IEnumerable<CommonAvatar> commonAvatars, AvatarStatus result)
    {
        foreach (var id in item.SupportedAvatars)
        {
            if (!id.StartsWith("commonavatar:")) continue;

            var group = commonAvatars.FirstOrDefault(g => g.Identifier == id);
            if (group?.Avatars.Contains(avatarId) is true)
            {
                result.IsCommon = true;
                result.CommonAvatarName = group.GroupName;
                return;
            }
        }
    }

    private static void CheckIndirectCommonAvatar(Item item, string avatarId, IEnumerable<CommonAvatar> commonAvatars, AvatarStatus result)
    {
        var groupsForPath = commonAvatars.Where(x => x.Avatars.Contains(avatarId));
        if (!groupsForPath.Any()) return;

        foreach (var supportedAvatar in item.SupportedAvatars)
        {
            var group = groupsForPath.FirstOrDefault(g => g.Avatars.Contains(supportedAvatar));
            if (group != null)
            {
                result.IsCommon = true;
                result.CommonAvatarName = group.GroupName;
                return;
            }
        }
    }
}
