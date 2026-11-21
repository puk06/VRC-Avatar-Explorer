using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// バリエーションごとのダウンロード可能ファイルのハッシュを保持するモデルクラスです。アイテムごとにバリエーションの更新差分を検出するために使用されます。
/// </summary>
public class VariationHash(string itemId) : AbstractDatabaseItem, IIdentifiable
{
#pragma warning disable RCS1170
    /// <summary>このハッシュが対応するアイテムの識別子（Identifier）です。</summary>
    [JsonInclude] public string ItemId { get; private set; } = itemId;
#pragma warning restore RCS1170

    /// <summary>バリエーションIDをキーとし、各バリエーションの情報（バリエーション名とそのバリエーションに含まれるダウンロード可能ファイルの一覧）を保持する不変辞書です。</summary>
    [JsonInclude] public ImmutableDictionary<string, VariationInfo> VariationInfos { get; private set; }
        = ImmutableDictionary<string, VariationInfo>.Empty;

    /// <summary>指定したバリエーションのファイル一覧を更新（または追加）します。</summary>
    /// <param name="variationId">更新対象のバリエーションID。</param>
    /// <param name="info">更新するバリエーション情報（バリエーション名とそのバリエーションに含まれるダウンロード可能ファイルの一覧）。</param>
    public void UpdateVariationInfo(string variationId, VariationInfo info)
    {
        VariationInfos = VariationInfos.SetItem(variationId, info);
    }

    /// <summary>すべてのバリエーションのファイル一覧を一括で上書きします。</summary>
    /// <param name="allInfos">バリエーションIDをキーとしたバリエーション情報のコレクション。既存のデータはすべて置き換えられます。</param>
    public void UpdateAllVariations(IEnumerable<KeyValuePair<string, VariationInfo>> allInfos)
    {
        VariationInfos = allInfos.ToImmutableDictionary();
    }

    /// <summary>このバリエーションハッシュを一意に識別する識別子（"variationHash:" + Id）です。</summary>
    [JsonIgnore] public string Identifier => "variationHash:" + Id;
}
