namespace AvatarExplorer.Core.Models.External;

public class ImportRequest
{
    public DataImportType ImportType { get; set; }
    public string DataFolderPath { get; set; } = string.Empty;
    public bool CopyAssetData { get; set; }
    public Func<(string Message, int Percent), Task>? ReportProgress { get; set; }
}
