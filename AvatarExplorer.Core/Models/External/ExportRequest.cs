namespace AvatarExplorer.Core.Models.External;

public class ExportRequest
{
    public DataExportType ExportType { get; set; } = DataExportType.Csv;
    public string FolderPath { get; set; } = string.Empty;
    public bool IncludeCommonToSupported { get; set; }
    public Func<(string, int), Task>? ReportProgress { get; set; }
}
