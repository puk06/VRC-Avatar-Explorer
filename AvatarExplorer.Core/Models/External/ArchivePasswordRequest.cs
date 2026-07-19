namespace AvatarExplorer.Core.Models.External;

public sealed record ArchivePasswordRequest
{
    public required string ArchivePath { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public int Attempt { get; init; }
    public string? ErrorMessage { get; init; }
}
