using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// アイテムの作者を表すモデルクラスです。ナビゲーションの「作者選択」で使用されます。
/// </summary>
public class Author : IIdentifiable
{
    /// <summary>作者名です。</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>この作者に属するアイテムの件数です。</summary>
    public int ItemCount { get; set; } = 0;

    /// <summary>この作者を一意に識別する識別子（"author:" + Name）です。</summary>
    public string Identifier => "author:" + Name;
}
