using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 一括インポートで使用するプリセットを表すモデルクラスです。複数の <see cref="BulkImportItem"/> をまとめて保持します。
/// </summary>
public class BulkImportPreset(string presetName) : AbstractDatabaseItem, IIdentifiable
{
    /// <summary>プリセット名です。</summary>
    [JsonInclude] public string PresetName { get; private set; } = presetName;
    /// <summary>このプリセットに含まれるインポート対象アイテムの一覧です。</summary>
    [JsonInclude] public ImmutableArray<BulkImportItem> Items { get; private set; } = [];

    /// <summary>プリセット名を更新します。</summary>
    /// <param name="presetName">新しいプリセット名。</param>
    public void UpdatePresetName(string presetName) => PresetName = presetName;
    /// <summary>含まれるインポート対象アイテム一覧を更新します。</summary>
    /// <param name="items">新しいインポート対象アイテム一覧。</param>
    public void UpdateItems(IEnumerable<BulkImportItem> items) => Items = items.ToImmutableArray();

    /// <summary>このプリセットを一意に識別する識別子（"bulkimportpreset:" + Id）です。</summary>
    public string Identifier => "bulkimportpreset:" + Id;
}
