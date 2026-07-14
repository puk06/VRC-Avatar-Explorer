using System.Text.RegularExpressions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Utils;

public static partial class ItemUtils
{
    [GeneratedRegex(@"\u3010[^\u3011]+\u3011")]
    private static partial Regex TextBracketsRegex();

    internal static string GetTitleFromDictionary(Dictionary<string, string> itemTitleMaps, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return string.Empty;
        return itemTitleMaps.TryGetValue(itemId, out string? avatarName) ? avatarName : string.Empty;
    }

    [Obsolete("v2.7.0からデータベースのパスがフルパスに移行するため、そのままItem.ItemPathをご利用ください。")]
    public static string GetItemPath(string parentFolder, string itemPath)
    {
        // <sys>で始まっていたら相対パスと認識して親フォルダに置き換える
        // 始まっていないものはフルパスと認識してそのまま変えす
        return itemPath.StartsWith("<sys>") ? Path.Join(parentFolder, itemPath.Replace("<sys>", string.Empty)) : itemPath;
    }

    public static string RemoveBrackets(string value) => TextBracketsRegex().Replace(value, string.Empty);

    public static string? GetSafeTitle(string itemTitle)
    {
        // パスに使用しても大丈夫な文字だけ残す
        return FileNameUtils.GetSafeTitle(itemTitle);
    }

    internal static Dictionary<string, string> GetItemTitleMaps(IEnumerable<Item> items, IEnumerable<TempAvatar> tempAvatars)
    {
        var itemTitleMaps = new Dictionary<string, string>();
        
        foreach (var item in items)
            itemTitleMaps.Add(item.Identifier, item.Title);
        
        foreach (var tempAvatar in tempAvatars)
            itemTitleMaps.Add(tempAvatar.Identifier, tempAvatar.AvatarName);

        return itemTitleMaps;
    }
}
