using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Updates;

namespace AvatarExplorer.Core.Models.System;

public record RuntimeSettings
{
    public string DataRootDirectory { get; init; } = SystemPath.DefaultItemsFolderPath;
    public string AutoBackupRootDirectory { get; init; } = SystemPath.BackupFolderPath;
    public bool RemoveOriginal { get; init; } = false;
    public bool ShouldLinkToOriginal { get; init; } = false;
    public int AutoBackupInterval { get; init; } = 5;
    public bool TreatEmptySupportedAvatarAsNone { get; init; } = false;
    public int MaxDegreeOfParallelism { get; init; } = 4;
    public bool CheckForUpdate { get; init; } = true;
    public UpdateChannel UpdateChannel { get; init; } = UpdateChannel.Stable;
}
