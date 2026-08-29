using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>アイテム内のファイルを分類するためのカテゴリタイプを表す列挙型です。拡張子やファイル名で自動分類されます。</summary>
public enum ItemFileCategoryType
{
    /// <summary>未分類。</summary>
    None,

    /// <summary>マテリアル（.unitypackage）。</summary>
    [FileNamesFilter("Material|マテリアル")]
    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(Loc.FileCategory.Material)]
    Material,

    /// <summary>Unitypackage（.unitypackage）。</summary>
    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(Loc.FileCategory.Unitypackage)]
    Unitypackage,

    /// <summary>画像テクスチャ（.png/.jpg/.webp 等）。</summary>
    [ExtensionsFilter(".png|.jpg|.jpeg|.webp|.tga|.bmp|.dds|.tiff|.tif|.gif|.hdr|.exr")]
    [LocalizationKey(Loc.FileCategory.Texture)]
    Texture,

    /// <summary>編集用データ（.psd/.blend/.fbx 等）。</summary>
    [ExtensionsFilter(".psd|.psb|.clip|.kra|.sai|.sai2|.blend|.fbx|.obj|.gltf|.glb|.dae|.stl")]
    [LocalizationKey(Loc.FileCategory.Modification)]
    Modification,

    /// <summary>ドキュメント（.txt/.md/.pdf 等）。</summary>
    [ExtensionsFilter(".txt|.md|.pdf|.rtf|.doc|.docx")]
    [LocalizationKey(Loc.FileCategory.Document)]
    Document,

    /// <summary>フォント（.ttf/.otf 等）。</summary>
    [ExtensionsFilter(".ttf|.otf|.woff|.woff2|.eot|.fon")]
    [LocalizationKey(Loc.FileCategory.Font)]
    Font,

    /// <summary>URLショートカット（.url）。</summary>
    [ExtensionsFilter(".url")]
    [LocalizationKey(Loc.FileCategory.UrlShortcut)]
    UrlShortcut,

    /// <summary>分類不能なその他のファイル。</summary>
    [LocalizationKey(Loc.FileCategory.Unknown)]
    Unknown
}
