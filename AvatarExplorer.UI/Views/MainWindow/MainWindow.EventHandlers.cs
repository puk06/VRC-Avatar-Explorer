using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    #region Left Filter
    private void Main_LeftFilter_SelectionChanged(object? sender, SelectionChangedEventArgs e) => Main_RenderLeftPanel();
    #endregion

    #region Main Top Buttons
    private void Main_SettingsButton_Click(object? sender, RoutedEventArgs e) => SettingsOverlay_Open();
    private void Main_UndoButton_Click(object? sender, RoutedEventArgs e) => Main_ExecuteUndo();
    private void Main_HomeButton_Click(object? sender, RoutedEventArgs e) => Main_ExecuteHome();
    private void Main_AddItem_Click(object? sender, RoutedEventArgs e) => AddItemOverlay_Open();
    #endregion

    #region Side Panel
    private void Main_ShowSidePanel_Click(object? sender, PointerPressedEventArgs e)
    {
        if (!Main_SidePanelBorder.IsVisible) SidePanel_Show();
        else SidePanel_Hide();
    }
    #endregion

    #region Drag and Drop
    private string _main_lastDragAndDropItem = string.Empty;
    private async void Main_ItemButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Button button) return;

        if (button.Tag is ItemTagInfo itemTagInfo)
        {
            DataTransferItem item = new();

            // ファイルの場合はUnityなどに対応するためにファイルをD&Dで追加する
            if (itemTagInfo.State == ItemTagStates.ItemFileCategoryOpen) item.Set(DataFormat.File, await StorageService.GetStorageFileFromPath(this, itemTagInfo.Value));
            else item.Set(DataFormat.Text, () => itemTagInfo.Value);

            _main_lastDragAndDropItem = itemTagInfo.Value;

            DataTransfer dragData = new();
            dragData.Add(item);

            await Task.Delay(300);

            // 300ms後もそのボタンがクリックされていたら、長押しとみなしてD&D処理を開始する
            if (!button.IsPressed) return;

            await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Copy);
        }
    }
    private void Main_DragDrop_Over(object? sender, DragEventArgs e)
    {
        // ファイルのD&D: File | アイテムボタンのD&D: Text
        if (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text)) e.DragEffects = DragDropEffects.Copy;
    }
    private void Main_DragDrop_Drop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;

        IEnumerable<IStorageItem?> storageItems = e.DataTransfer.GetItems(DataFormat.File).Select(i => i.TryGetFile());
        if (storageItems == null) return;

        string[] storageItemPaths = storageItems
            .Select(i => i?.TryGetLocalPath())
            .Where(i => !string.IsNullOrEmpty(i) && (Directory.Exists(i) || File.Exists(i)))
            .ToArray()!;

        // ソフト内からD&Dしたアイテムはスキップするように
        if (storageItemPaths.Length == 1 && storageItemPaths[0] == _main_lastDragAndDropItem) return;

        AddItemOverlay_Open(storageItemPaths);
    }
    #endregion

    #region Mouse Event
    private void Main_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties pointerProperties = e.GetCurrentPoint(this).Properties;
        if (pointerProperties.IsXButton1Pressed) Main_ExecuteUndo(); // XButton1はマウスの横ボタンなので戻る処理を実行する
    }
    #endregion

    #region Window Closing
    private void Main_Closing(object? sender, WindowClosingEventArgs e) => AvatarExplorerApp.ClearTemp();
    #endregion
}
