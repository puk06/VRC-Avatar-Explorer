using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// アイテム内の個々のファイルを表すモデルクラスです。ナビゲーションの末端（ファイル選択）で使用されます。
/// </summary>
public class ItemFile(string folderPath, string filePath) : IIdentifiable
{
    /// <summary>このファイルが属する親フォルダのフルパスです。</summary>
    public string ParentFolderPath { get; } = folderPath;
    /// <summary>このファイルが属する親フォルダの名前です。</summary>
    public string ParentFolderName { get; } = Path.GetFileName(folderPath) ?? string.Empty;

    /// <summary>ファイルのフルパスです。</summary>
    public string FilePath { get; } = filePath;
    /// <summary>ファイル名です。</summary>
    public string FileName { get; } = Path.GetFileName(filePath) ?? string.Empty;
    /// <summary>ファイルの拡張子（先頭の "." を除いた大文字表記）です。拡張子がない場合は空文字になります。</summary>
    public string Extension { get; } = string.IsNullOrEmpty(Path.GetExtension(filePath)) ? string.Empty : Path.GetExtension(filePath)[1.. ].ToUpper();

    /// <summary>このファイルを一意に識別する識別子（"file:" + パスのハッシュ）です。</summary>
    public string Identifier => "file:" + PathUtils.ComputeHash(FilePath);
}
