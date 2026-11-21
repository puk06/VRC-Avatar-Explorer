using System.Collections.Immutable;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// バリエーションの情報を表すレコードです。バリエーション名とそのバリエーションに含まれるダウンロード可能ファイルの一覧を保持します。
/// </summary>
/// <param name="Name">バリエーション名。</param>
/// <param name="Files">そのバリエーションに含まれるダウンロード可能ファイルの不変配列。</param>
public record VariationInfo(string Name, ImmutableArray<DownloadableFile> Files)
{
    /// <summary>未初期化（<see langword="default"/>）の場合は空の不変配列に正規化します。</summary>
    public ImmutableArray<DownloadableFile> Files { get; init; } = Files.IsDefault ? [] : Files;
}
