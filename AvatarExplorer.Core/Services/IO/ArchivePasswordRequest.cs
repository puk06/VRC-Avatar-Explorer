namespace AvatarExplorer.Core.Services.IO;

public sealed record ArchivePasswordRequest
{
    public required string ArchivePath { get; init; }
    public int Attempt { get; init; }
    public bool IsRetry => Attempt > 1;
    public string? ErrorMessage { get; init; }
}
