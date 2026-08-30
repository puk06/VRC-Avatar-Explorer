namespace AvatarExplorer.UI.Models.ContextMenu;

public enum ActionKey
{
    None,

    CheckForUpdate,
    CopyBoothLink,
    OpenBoothLink,
    ShowOtherItemsByAuthor,
    ChangeThumbnail,
    FetchThumbnail,
    CopyItemInfo,
    EditItem,
    EditItemTitle,
    EditItemMemo,
    AddToBulkImportList,
    AddItemFile,
    AddItemFolder,
    EditImplementedAvatar,
    EditItemDefaultPath,
    EditItemTag,
    RemoveItem,

    OpenFolder,
    ShowInExplorer, // Fileにも使われる
    RemoveFolder,

    OpenFile,
    AddFileToBulkImportList,
    OpenUnitypackageViewer,
    OpenPdfViewer,

    RemovePreset,

    EditTempAvatarName,
    ResolveTempAvatar,
    RemoveTempAvatar,

    EditCustomCategoryName,
    MergeWithOtherCategory,

    HideItem,
    ShowItem,

    IncludeInCommonAvatarCheck,
    SkipIndirectCommonAvatarCheck
}
