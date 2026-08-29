namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// データのインポート時に指定する要求情報を表します。
/// </summary>
public class ImportRequest
{
    /// <summary>
    /// インポート元の種類とインポート対象を組み合わせたフラグ。
    /// </summary>
    public DataImportType ImportType { get; set; }

    /// <summary>
    /// インポート元となるデータフォルダのパス。
    /// </summary>
    public string DataFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// アセットデータをコピーするかどうか。false の場合は元データへのリンクが作成されます。
    /// </summary>
    public bool CopyAssetData { get; set; }

    /// <summary>
    /// インポートの進捗を報告するコールバック。
    /// </summary>
    public Func<(string Message, int Percent), Task>? ReportProgress { get; set; }
}
