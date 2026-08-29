namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// データのエクスポート形式を表します。
/// </summary>
public enum DataExportType
{
    /// <summary>
    /// エクスポートなし / 未指定。
    /// </summary>
    None,

    /// <summary>
    /// CSV形式でエクスポートします。
    /// </summary>
    Csv,

    /// <summary>
    /// KonoAsset形式でエクスポートします。
    /// </summary>
    KonoAsset
}
