using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.System;

public record RuntimeSettings
{
    public string DataRootDirectory { get; init; } = SystemPath.DefaultItemsFolderPath;
    public string AutoBackupRootDirectory { get; init; } = SystemPath.BackupFolderPath;
    public ItemSortOrder ItemSortOrder { get; init; } = ItemSortOrder.Updated;
    public bool RemoveOriginal { get; init; } = false;
    public bool RemoveBrackets { get; init; } = false; // TODO: これはUIで管理すべき
    public bool ShouldLinkToOriginal { get; init; } = false;
    public int AutoBackupInterval { get; init; } = 5;
    public bool TreatEmptySupportedAvatarAsNone { get; init; } = false;
    public int MaxDegreeOfParallelism { get; init; } = 4;
}
