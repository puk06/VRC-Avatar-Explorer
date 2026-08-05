namespace AvatarExplorer.Core.Models.External;

[Flags]
public enum DataImportType
{
    None = 0,
    V1 = 1,
    KonoAsset = 2,
    SourceMask = V1 | KonoAsset,
    Items = 8,
    Thumbnails = 16
}
