namespace AvatarExplorer.Core.Models.External;

public sealed record ArchivePasswordRequest
{
    public required string ArchivePath { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public int Attempt { get; init; }
    public bool IsRetry => Attempt > 1;
    public string? ErrorMessage { get; init; }
}
