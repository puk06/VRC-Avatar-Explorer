using System.Text.Json.Serialization;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 一括インポートプリセットに含まれる個別のアイテムとそのファイルパスを表すクラスです。
/// </summary>
public class BulkImportItem(string itemId, string filePath)
{
    /// <summary>対象となるアイテムの識別子（Identifier）です。</summary>
    [JsonInclude] public string ItemId { get; private set; } = itemId;
    /// <summary>インポート対象のファイルパスです。</summary>
    [JsonInclude] public string FilePath { get; private set; } = filePath;

    /// <summary>対象アイテムの識別子を更新します。</summary>
    /// <param name="itemId">新しいアイテム識別子。</param>
    public void UpdateItemId(string itemId) => ItemId = itemId;
    /// <summary>インポート対象のファイルパスを更新します。</summary>
    /// <param name="path">新しいファイルパス。</param>
    public void UpdateItemPath(string path) => FilePath = path;
}
