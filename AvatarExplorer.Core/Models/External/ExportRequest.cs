using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// データのエクスポート時に指定する要求情報を表します。
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// エクスポートの形式（CSV / KonoAsset など）。
    /// </summary>
    public DataExportType ExportType { get; set; } = DataExportType.Csv;

    /// <summary>
    /// 出力先フォルダのパス。
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// 共通素体グループを各アイテムの対応アバターとして展開して出力するかどうか。
    /// </summary>
    public bool IncludeCommonToSupported { get; set; }

    /// <summary>
    /// カテゴリ名（ItemType）を表示用の文字列に変換するローカライズ関数。
    /// </summary>
    public Func<ItemType, ValueTask<string?>>? ItemTypeLocalizer { get; set; }

    /// <summary>
    /// エクスポートの進捗を報告するコールバック。
    /// </summary>
    public Func<(string Message, int Percent), Task>? ReportProgress { get; set; }
}
