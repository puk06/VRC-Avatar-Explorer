namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// Boothのダウンロード可能ファイルを表すレコードです。ファイル名とそのハッシュを保持します。
/// </summary>
/// <param name="FileName">ダウンロードファイルの名前。</param>
/// <param name="Hash">ファイルのハッシュ値（バリエーション管理で使用）。</param>
public record DownloadableFile(string FileName, string Hash);
