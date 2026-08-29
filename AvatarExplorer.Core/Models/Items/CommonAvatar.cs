using System.Collections.Immutable;
using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 複数のアバターを一つのグループとしてまとめる「共通素体」を表すモデルクラスです。グループに対応アバターを設定すると、グループ内の全てのアバターに対応した扱いになります。
/// </summary>
public class CommonAvatar(string groupName) : AbstractDatabaseItem, IIdentifiable
{
    /// <summary>共通素体グループの名前です。</summary>
    [JsonInclude] public string GroupName { get; private set; } = groupName;
    /// <summary>このグループに含まれるアバターの識別子（Identifier）一覧です。</summary>
    [JsonInclude] public ImmutableArray<string> Avatars { get; private set; } = [];

    /// <summary>グループ名を更新します。</summary>
    /// <param name="newName">新しいグループ名。</param>
    public void UpdateGroupName(string newName) => GroupName = newName;
    /// <summary>グループに含まれるアバター一覧を更新します。重複は除去されます。</summary>
    /// <param name="avatars">新しいアバターの識別子一覧。</param>
    public void UpdateAvatars(IEnumerable<string> avatars) => Avatars = avatars.Distinct().ToImmutableArray();

    /// <summary>この共通素体グループを一意に識別する識別子（"commonavatar:" + Id）です。</summary>
    [JsonIgnore] public string Identifier => "commonavatar:" + Id;
}
