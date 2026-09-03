using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// アイテムのパス変換やファイル名の安全性チェックなど、アイテム関連のユーティリティを提供します。
/// </summary>
public static partial class ItemUtils
{
    /// <summary>
    /// データルートディレクトリを表す特殊なプレフィックス（&lt;root&gt;）です。
    /// </summary>
    public const string RootFolderPrefix = "<root>";
    internal static string GetTitleFromDictionary(Dictionary<string, string> itemTitleMaps, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return string.Empty;
        return itemTitleMaps.TryGetValue(itemId, out string? avatarName) ? avatarName : string.Empty;
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

    /// <summary>
    /// 指定したパスが、アプリが管理しているアイテムフォルダ配下の直下サブディレクトリかどうかを判定します。
    /// </summary>
    /// <param name="itemPath">アプリが管理しているアイテムのルートパス。</param>
    /// <param name="path">判定対象のパス。</param>
    /// <returns>管理されているパスの場合は true。itemPath が空/null または存在しない場合は false。</returns>
    public static bool IsAppManagedPath(string itemPath, string path)
    {
        if (string.IsNullOrEmpty(itemPath) || !Directory.Exists(itemPath))
            return false;

        return Directory.GetDirectories(itemPath, "*", SearchOption.TopDirectoryOnly)
            .Contains(path);
    }

    /// <summary>
    /// アイテムの相対パス（&lt;root&gt; プレフィックス付き）を、実際のフルパスに変換します。
    /// </summary>
    /// <param name="itemPath">アイテムのパス文字列。</param>
    /// <param name="rootDirectory">データルートディレクトリ。省略時はランタイム設定の値を使用します。</param>
    /// <returns>変換後のフルパス。変換できない場合は元の itemPath をそのまま返します。</returns>
    public static string GetFullPath(string itemPath, string? rootDirectory = null)
    {
        rootDirectory ??= AvatarExplorerApp.Instance.RuntimeSettings.DataRootDirectory;
        if (string.IsNullOrEmpty(rootDirectory)) return itemPath;

        if (itemPath.StartsWith(RootFolderPrefix))
            return Path.Join(rootDirectory, itemPath.AsSpan(RootFolderPrefix.Length));

        return itemPath;
    }

    /// <summary>
    /// アイテムのフルパスを、&lt;root&gt; プレフィックスを使った相対パスに変換します。
    /// </summary>
    /// <param name="itemPath">アイテムのフルパス文字列。</param>
    /// <param name="rootDirectory">データルートディレクトリ。省略時はランタイム設定の値を使用します。</param>
    /// <returns>変換後の相対パス。ルート配下でない場合は元の itemPath をそのまま返します。</returns>
    public static string GetRelativePath(string itemPath, string? rootDirectory = null)
    {
        rootDirectory ??= AvatarExplorerApp.Instance.RuntimeSettings.DataRootDirectory;
        if (string.IsNullOrEmpty(rootDirectory)) return itemPath;

        if (!itemPath.StartsWith(rootDirectory)) return itemPath;

        return RootFolderPrefix + Path.DirectorySeparatorChar + Path.GetRelativePath(rootDirectory, itemPath);
    }
}
