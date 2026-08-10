using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Utils;

public static partial class ItemUtils
{
    internal static string GetTitleFromDictionary(Dictionary<string, string> itemTitleMaps, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return string.Empty;
        return itemTitleMaps.TryGetValue(itemId, out string? avatarName) ? avatarName : string.Empty;
    }

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

    public static bool IsAppManagedPath(string itemPath, string path)
    {
        if (string.IsNullOrEmpty(itemPath) || !Directory.Exists(itemPath))
            return false;

        return Directory.GetDirectories(itemPath, "*", SearchOption.TopDirectoryOnly)
            .Contains(path);
    }
}
