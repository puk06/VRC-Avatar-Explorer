using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// ナビゲーションで使用されるフォルダを表すモデルクラスです。カテゴリ別フォルダやアイテム内のフォルダ・拡張子別フォルダなどに使用されます。
/// </summary>
public class Folder(string identifier, string? path = null) : IIdentifiable
{
    /// <summary>フォルダの表示タイトルです。</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>タイトルをローカライズ可能かどうかを示します。</summary>
    public bool TitleLocalizable { get; set; } = false;
    /// <summary>このフォルダに含まれるアイテム（またはファイル）の件数です。</summary>
    public int ItemCount { get; set; } = 0;
    /// <summary>このフォルダを一意に識別する識別子です。</summary>
    public string Identifier => identifier;
    /// <summary>このフォルダのフルパスです。パスが解決されていない場合は null になります。</summary>
    public string? Path { get; } = path;
}
