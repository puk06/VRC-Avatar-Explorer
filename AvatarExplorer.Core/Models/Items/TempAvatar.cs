using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// まだ正式に登録されていないアバターを一時的に識別するための「仮アバター」を表すモデルクラスです。後で正式なアバターに「解決（Resolve）」できます。
/// </summary>
public class TempAvatar(string avatarName, int boothId = -1) : AbstractDatabaseItem, IIdentifiable
{
    /// <summary>仮アバターの名前です。</summary>
    [JsonInclude] public string AvatarName { get; private set; } = avatarName;

    /// <summary>仮アバターのBoothIdです。未設定の場合は -1 です。</summary>
    [JsonInclude] public int BoothId { get; set; } = boothId;

    /// <summary>仮アバター名を更新します。</summary>
    /// <param name="newName">新しいアバター名。</param>
    public void UpdateAvatarName(string newName) => AvatarName = newName;

    /// <summary> 仮アバターのBoothIdを更新します。</summary>
    /// <param name="newBoothId">新しいBoothId。</param>
    public void UpdateBoothId(int newBoothId) => BoothId = newBoothId;

    /// <summary>この仮アバターを一意に識別する識別子（"tempavatar:" + Id）です。</summary>
    [JsonIgnore] public string Identifier => "tempavatar:" + Id;
}
