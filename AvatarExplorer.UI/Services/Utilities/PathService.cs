using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.Services.Utilities;

internal static class PathService
{
    internal static string BuildPath(IEnumerable<Item> items, IEnumerable<TempAvatar> tempAvatars, SelectionNode selectionNode, bool removeBrackets)
    {
        ItemTagStates state = selectionNode.State;
        string value = selectionNode.Key;

        if (StateFlagUtils.IsItemState(state))
        {
            if (value.StartsWith(TempAvatar.InternalPathPrefix))
            {
                TempAvatar? tempAvatar = tempAvatars.FirstOrDefault(i => i.GetInternalId() == value);
                if (tempAvatar == null) value = Localizer.Instance[LocalizationKey.Main.Path.Removed]; // 見つからない時は削除済みと表記する
                else value = tempAvatar.AvatarName;
            }
            else
            {
                Item? item = items.FirstOrDefault(i => i.Id == value);
                if (item == null) value = Localizer.Instance[LocalizationKey.Main.Path.Removed]; // 見つからない時は削除済みと表記する
                else value = removeBrackets ? ItemUtils.RemoveBrackets(item.Title) : item.Title; // アイテムはパスからタイトルに変換する
            }
        }

        if (StateFlagUtils.IsCategoryState(state))
        {
            // カテゴリはValue自体を翻訳する
            // カテゴリ: Search.Category.Textureのような感じで入っているため
            value = Localizer.Instance[value];
        }

        if (state == ItemTagStates.ItemFolder)
        {
            // フォルダはRootかどうかで表記を変える
            value = value == ItemFolder.RootNodeName ? Localizer.Instance[LocalizationKey.Main.Path.RootFolder] : value;
        }

        // 翻訳できないタグ(Root以外)はここがnullになるため、valueがパスになる。ある場合はPrefixが翻訳される。
        string? localizationKey = state.GetLocalizationKey();

        return localizationKey == null ? value : Localizer.Instance.Get(localizationKey, value);
    }
}
