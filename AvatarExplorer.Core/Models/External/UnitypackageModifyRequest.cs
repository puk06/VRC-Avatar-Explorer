namespace AvatarExplorer.Core.Models.External;

public class UnitypackageModifyRequest
{
    public required IReadOnlyList<UnitypackageImportEntry> Entries { get; init; }
    public bool? ChangeUnitypackagePath { get; init; }
    public Func<(string Message, int Percent), Task>? ReportProgress { get; init; }
}
