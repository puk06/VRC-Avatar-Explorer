using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External;

public class ExportRequest
{
    public DataExportType ExportType { get; set; } = DataExportType.Csv;
    public string FolderPath { get; set; } = string.Empty;
    public bool IncludeCommonToSupported { get; set; }
    public Func<ItemType, ValueTask<string?>>? ItemTypeLocalizer { get; set; }
    public Func<(string Message, int Percent), Task>? ReportProgress { get; set; }
}
