namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// バリエーションの情報を表すレコードです。バリエーション名とそのバリエーションに含まれるダウンロード可能ファイルのリストを保持します。
/// </summary>
/// <param name="Name">バリエーション名。</param>
/// <param name="Files">そのバリエーションに含まれるダウンロード可能ファイルのリスト。</param>
public record VariationInfo(string Name, List<DownloadableFile> Files);
