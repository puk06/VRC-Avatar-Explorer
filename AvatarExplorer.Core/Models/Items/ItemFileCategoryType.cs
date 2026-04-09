using AvatarExplorer.Core.Attributes;
using AvatarExplorer.Core.Localization;

namespace AvatarExplorer.Core.Models.Items;

public enum ItemFileCategoryType
{
    None,

    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(LocalizationKey.FileCategory.Unitypackage)]
    Unitypackage,

    [FileNamesFilter("Material|マテリアル")]
    [ExtensionsFilter(".unitypackage")]
    [LocalizationKey(LocalizationKey.FileCategory.Material)]
    Material,

    [ExtensionsFilter(".png|.jpg|.jpeg|.webp|.tga|.bmp|.dds|.tiff|.tif|.gif|.hdr|.exr")]
    [LocalizationKey(LocalizationKey.FileCategory.Texture)]
    Texture,

    [ExtensionsFilter(".psd|.psb|.clip|.kra|.sai|.sai2|.blend|.fbx|.obj|.gltf|.glb|.dae|.stl")]
    [LocalizationKey(LocalizationKey.FileCategory.Modification)]
    Modification,

    [ExtensionsFilter(".txt|.md|.pdf|.rtf|.doc|.docx")]
    [LocalizationKey(LocalizationKey.FileCategory.Document)]
    Document,

    [ExtensionsFilter(".ttf|.otf|.woff|.woff2|.eot|.fon")]
    [LocalizationKey(LocalizationKey.FileCategory.Font)]
    Font,

    [ExtensionsFilter(".url")]
    [LocalizationKey(LocalizationKey.FileCategory.UrlShortcut)]
    UrlShortcut,

    [LocalizationKey(LocalizationKey.FileCategory.Unknown)]
    Unknown
}
