namespace AvatarExplorer.UI.Models.ContextMenu;

public enum ActionKey
{
    None,

    ShowInMainView,

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
    AddItemUrl,
    MarkItemAsImplemented,
    MarkItemAsNotImplemented,
    ManageImplementedAvatars,
    EditItemDefaultPath,
    EditItemTag,
    RemoveItem,

    CopyPath, // Fileにも使われる
    OpenFolder,
    ShowInExplorer, // Fileにも使われる
    RemoveFolder,

    OpenFile,
    AddFileToBulkImportList,
    OpenUnitypackageViewer,
    OpenPdfViewer,

    RemovePreset,

    EditTempAvatarName,
    EditBoothId,
    ResolveTempAvatar,
    RemoveTempAvatar,

    EditCustomCategoryName,
    MergeWithOtherCategory,

    HideItem,
    ShowItem,

    EnableIndirectCommonAvatarCheck,
    SkipIndirectCommonAvatarCheck
}
