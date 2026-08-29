namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// パスワード保護されたアーカイブの解凍時に使用する、パスワード入力要求を表します。
/// </summary>
public sealed record ArchivePasswordRequest
{
    /// <summary>
    /// パスワード入力を求めているアーカイブファイルのパス。
    /// </summary>
    public required string ArchivePath { get; init; }

    /// <summary>
    /// パスワード入力の最大許容試行回数。
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// 現在の試行回数（0から始まる）。
    /// </summary>
    public int Attempt { get; init; }

    /// <summary>
    /// 前回の試行で失敗した場合に設定されるエラーメッセージ。
    /// </summary>
    public string? ErrorMessage { get; init; }
}
