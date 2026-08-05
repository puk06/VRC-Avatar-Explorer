namespace AvatarExplorer.Core.Models.External;

public class ImportRequest
{
    public DataImportType ImportType { get; set; }
    public string DataFolderPath { get; set; } = string.Empty;
    public bool CopyAssetData { get; set; }
    public Func<(string, int), Task>? ReportProgress { get; set; }
}
