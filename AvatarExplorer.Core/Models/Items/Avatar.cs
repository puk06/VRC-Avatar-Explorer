using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>アバターの種別を表す列挙型です。</summary>
public enum AvatarType
{
    /// <summary>未設定。</summary>
    None,
    /// <summary>通常のアイテム（アバター）。</summary>
    Item,
    /// <summary>共通素体グループ。</summary>
    CommonAvatar,
    /// <summary>仮アバター。</summary>
    TempAvatar
}

/// <summary>
/// 通常のアイテム（アバター）、共通素体、仮アバターを統一的に扱うためのラッパークラスです。ナビゲーションでは "avatar:" プレフィックス付きの識別子として扱われます。
/// </summary>
public class Avatar : IIdentifiable
{
    /// <summary>このアバターの種別（通常アイテム / 共通素体 / 仮アバター）です。</summary>
    public AvatarType Type { get; } = AvatarType.None;
    /// <summary>ラップされている元のオブジェクト（Item / CommonAvatar / TempAvatar）です。</summary>
    public IIdentifiable Item { get; }
    /// <summary>識別子に "avatar:" プレフィックスを付けるかどうかを示します。true の場合は付きません。</summary>
    public bool RawIdentifier { get; } // avatar:を付けるかどうかです

    /// <summary>ラップするオブジェクトから Avatar を初期化します。</summary>
    /// <param name="navigationable">ラップする IIdentifiable オブジェクト（Item / CommonAvatar / TempAvatar）。</param>
    /// <param name="rawIdentifier">true の場合、識別子に "avatar:" プレフィックスを付けません。</param>
    public Avatar(IIdentifiable navigationable, bool rawIdentifier = false)
    {
        Item = navigationable;
        RawIdentifier = rawIdentifier;

        if (navigationable is Item) Type = AvatarType.Item;
        else if (navigationable is CommonAvatar) Type = AvatarType.CommonAvatar;
        else if (navigationable is TempAvatar) Type = AvatarType.TempAvatar;
    }

    /// <summary>このアバターの識別子です。<see cref="RawIdentifier"/> が false の場合は "avatar:" + 元の識別子、true の場合は元の識別子そのままになります。</summary>
    public string Identifier => RawIdentifier ? Item.Identifier : "avatar:" + Item.Identifier;
}
