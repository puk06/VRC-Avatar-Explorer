using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// Boothのバリエーションごとのダウンロード可能ファイルのハッシュを保持するモデルクラスです。アイテムごとにバリエーションの更新差分を検出するために使用されます。
/// </summary>
public class VariationHash(string itemId) : AbstractDatabaseItem, IIdentifiable
{
#pragma warning disable RCS1170
    /// <summary>このハッシュが対応するアイテムの識別子（Identifier）です。</summary>
    [JsonInclude] public string ItemId { get; private set; } = itemId;
#pragma warning restore RCS1170

    /// <summary>バリエーションIDをキーとし、各バリエーションに含まれるダウンロード可能ファイルのリストを値とする辞書です。</summary>
    [JsonInclude] public Dictionary<string, List<DownloadableFile>> VariationFiles { get; private set; } = [];

    /// <summary>指定したバリエーションのファイル一覧を更新（または追加）します。</summary>
    /// <param name="variationId">更新対象のバリエーションID。</param>
    /// <param name="files">そのバリエーションに含まれるファイル一覧。</param>
    public void UpdateVariationFiles(string variationId, List<DownloadableFile> files)
    {
        VariationFiles[variationId] = files;
    }

    /// <summary>すべてのバリエーションのファイル一覧を一括で上書きします。</summary>
    /// <param name="allFiles">バリエーションIDをキーとしたファイル一覧の辞書。</param>
    public void UpdateAllVariations(Dictionary<string, List<DownloadableFile>> allFiles)
    {
        VariationFiles = allFiles;
    }

    /// <summary>このバリエーションハッシュを一意に識別する識別子（"variationHash:" + Id）です。</summary>
    [JsonIgnore] public string Identifier => "variationHash:" + Id;
}
