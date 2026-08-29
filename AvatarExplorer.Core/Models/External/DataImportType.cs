namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// データのインポート元の種類と、インポートする対象を組み合わせて指定するフラグ列挙型。
/// </summary>
[Flags]
public enum DataImportType
{
    /// <summary>
    /// インポートなし / 未指定。
    /// </summary>
    None = 0,

    /// <summary>
    /// AvatarExplorer V1 形式からのインポート。
    /// </summary>
    V1 = 1,

    /// <summary>
    /// KonoAsset 形式からのインポート。
    /// </summary>
    KonoAsset = 2,

    /// <summary>
    /// フォルダからの直接インポート。
    /// </summary>
    Folder = 4,

    /// <summary>
    /// インポート元（ソース）を表すフラグのマスク（V1 | KonoAsset | Folder）。
    /// </summary>
    SourceMask = V1 | KonoAsset | Folder,

    /// <summary>
    /// アイテムをインポートする。
    /// </summary>
    Items = 8,

    /// <summary>
    /// サムネイルをインポートする。
    /// </summary>
    Thumbnails = 16
}
