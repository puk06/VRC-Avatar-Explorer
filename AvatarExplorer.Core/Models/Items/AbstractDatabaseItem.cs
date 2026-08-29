using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.Database;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// データベースに保存されるアイテムの基底クラスです。一意の <see cref="Id"/> を保持します。
/// </summary>
public abstract class AbstractDatabaseItem : IDatabaseItem
{
#pragma warning disable RCS1170
    /// <summary>このアイテムの一意のID（Guid文字列）です。インスタンス生成時に自動で割り当てられます。</summary>
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
#pragma warning restore RCS1170
}
