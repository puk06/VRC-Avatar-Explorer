using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Models.External;

public class ImportRequest
{
    public DataImportType ImportType { get; set; }
    public string DataFolderPath { get; set; } = string.Empty;
    public bool CopyAssetData { get; set; }
    public RuntimeSettings RuntimeSettings { get; set; } = new();
    public Func<(string, int), Task>? ReportProgress { get; set; }
}
