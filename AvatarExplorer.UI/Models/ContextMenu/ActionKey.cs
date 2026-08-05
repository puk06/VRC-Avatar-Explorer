namespace AvatarExplorer.UI.Models.ContextMenu;

public enum ActionKey
{
    None,
    
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

    OpenFile,
    AddFileToBulkImportList,
    OpenUnitypackageViewer,
    OpenPdfViewer,

    RemovePreset,

    EditTempAvatarName,
    ResolveTempAvatar,
    RemoveTempAvatar,

    EditCustomCategoryName,
    MergeWithOtherCategory
}
