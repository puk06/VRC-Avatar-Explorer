namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 個別のバリエーションの更新情報を表すレコードです。バリエーション名とその差分を保持します。
/// </summary>
/// <param name="VariationName">バリエーション名。</param>
/// <param name="Diff">そのバリエーションのファイル差分（追加・削除・変更）。</param>
public record VariationUpdateInfo(string VariationName, VariationDiff Diff);
