using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum ItemFileCategoryType
{
    None,

    [FileNamesFilter("Material|マテリアル")]
    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(Loc.FileCategory.Material)]
    Material,

    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(Loc.FileCategory.Unitypackage)]
    Unitypackage,

    [ExtensionsFilter(".png|.jpg|.jpeg|.webp|.tga|.bmp|.dds|.tiff|.tif|.gif|.hdr|.exr")]
    [LocalizationKey(Loc.FileCategory.Texture)]
    Texture,

    [ExtensionsFilter(".psd|.psb|.clip|.kra|.sai|.sai2|.blend|.fbx|.obj|.gltf|.glb|.dae|.stl")]
    [LocalizationKey(Loc.FileCategory.Modification)]
    Modification,

    [ExtensionsFilter(".txt|.md|.pdf|.rtf|.doc|.docx")]
    [LocalizationKey(Loc.FileCategory.Document)]
    Document,

    [ExtensionsFilter(".ttf|.otf|.woff|.woff2|.eot|.fon")]
    [LocalizationKey(Loc.FileCategory.Font)]
    Font,

    [ExtensionsFilter(".url")]
    [LocalizationKey(Loc.FileCategory.UrlShortcut)]
    UrlShortcut,

    [LocalizationKey(Loc.FileCategory.Unknown)]
    Unknown
}
