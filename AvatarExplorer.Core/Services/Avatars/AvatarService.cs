using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Avatars;

/// <summary>
/// 対応アバターIDの列挙に関するユーティリティを提供する静的クラスです。
/// 共通素体グループ（<c>commonavatar:</c>）の展開などを行います。
/// </summary>
public static class AvatarService
{
    /// <summary>
    /// 指定した対応アバターIDの列挙を展開し、実際のアバターIDの配列を取得します。
    /// <c>commonavatar:</c> で始まるIDは、該当する共通素体グループに含まれる個々のアバターIDに展開されます。
    /// </summary>
    /// <param name="avatars">展開対象の対応アバターID一覧（アイテム、共通素体の混合可）。</param>
    /// <param name="commonAvatars">共通素体グループの一覧。</param>
    /// <param name="includeCommonAvatarToSupported">
    /// <c>true</c> の場合、通常のアバターIDが含まれていれば、そのアバターが属する全共通素体グループのアバターも展開結果に含めます。
    /// </param>
    /// <returns>重複を除いた展開済みのアバターID配列。</returns>
    public static string[] GetAllSupportedAvatarIds(IEnumerable<string> avatars, IEnumerable<CommonAvatar> commonAvatars, bool includeCommonAvatarToSupported = false)
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

        return avatarIds.ToArray();
    }
}
