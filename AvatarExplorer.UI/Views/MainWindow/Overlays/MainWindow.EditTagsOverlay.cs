using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Utils;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private readonly List<string> _editTagsOverlay_selectedTags = new();

    private void EditTagsOverlay_Show(IReadOnlyList<string>? tags = null)
    {
        EditTagsOverlay.IsVisible = true;
        EditTagsOverlay_TagTextBox.Text = string.Empty;
        EditTagsOverlay_InitializeList(tags);
    }
    private void EditTagsOverlay_Hide() => EditTagsOverlay.IsVisible = false;

    private void EditTagsOverlay_InitializeList(IReadOnlyList<string>? tags = null)
    {
        _editTagsOverlay_selectedTags.Clear();
        if (tags != null) _editTagsOverlay_selectedTags.AddRange(tags);

        EditTagsOverlay_RefleshList();
        EditTagsOverlay_ReloadTagList();
    }
    private void EditTagsOverlay_RefleshList()
    {
        EditTagsOverlay_TagComboBox.Items.Clear();
        EditTagsOverlay_TagComboBox.Items.AddRange(
            _avatarExplorerApp.GetAllItems()
                .SelectMany(i => i.TagsView)
                .Distinct()
                .Select(i => new ComboBoxItem() { Content = i })
        );
    }
    private void EditTagsOverlay_ReloadTagList()
    {
        EditTagsOverlay_TagList.Children.Clear();

        foreach (string tag in _editTagsOverlay_selectedTags)
        {
            Border tagBorder = ItemButtonFactory.GetTagBorder(tag);
            if (tagBorder.Child is TextBlock tagLabel)
            {
                tagLabel.FontWeight = FontWeight.Bold;
                tagLabel.Classes.Add("accent");
            }

            tagBorder.Classes.Add("tagborder");
            tagBorder.PointerPressed += EditTagsOverlay_Tag_Click;

            EditTagsOverlay_TagList.Children.Add(tagBorder);
        }

        EditTagsOverlay_TagListScrollViewer.Offset = AvaloniaVectorUtils.MaxValue;
    }

    #region Event Handler
    private void EditTagsOverlay_Tag_Click(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Child is TextBlock taglabel && taglabel.Text is string tag)
        {
            _editTagsOverlay_selectedTags.RemoveAll(i => i == tag);
            EditTagsOverlay_ReloadTagList();
        }
    }
    private void EditTagsOverlay_TagTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) EditTagsOverlay_AddTagByText();
        else if (e.Key == Key.Escape) EditTagsOverlay_TagTextBox.Text = string.Empty;
    }
    private void EditTagsOverlay_AddTagButton_Click(object? sender, RoutedEventArgs e) => EditTagsOverlay_AddTagByText();
    private void EditTagsOverlay_AddTagByText()
    {
        if (!string.IsNullOrEmpty(EditTagsOverlay_TagTextBox.Text) && !_editTagsOverlay_selectedTags.Contains(EditTagsOverlay_TagTextBox.Text))
        {
            _editTagsOverlay_selectedTags.Add(EditTagsOverlay_TagTextBox.Text);
        }

        EditTagsOverlay_ReloadTagList();
        EditTagsOverlay_TagTextBox.Text = string.Empty;
    }
    private void EditTagsOverlay_TagComboBox_SelectionChanged(object? sender, RoutedEventArgs e)
    {
        string? selectedTag = (EditTagsOverlay_TagComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (string.IsNullOrEmpty(selectedTag) || _editTagsOverlay_selectedTags.Contains(selectedTag))
        {
            EditTagsOverlay_TagComboBox.SelectedIndex = -1;
            return;
        }

        _editTagsOverlay_selectedTags.Add(selectedTag);
        EditTagsOverlay_ReloadTagList();

        EditTagsOverlay_TagComboBox.SelectedIndex = -1;
    }
    private void EditTagsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditTagsOverlay_Hide();
    private void EditTagsOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = _avatarExplorerApp.GetItemById(_contextMenu_selectedItemId);
        if (item == null)
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.UpdateTags(_editTagsOverlay_selectedTags);
        _avatarExplorerApp.UpdateSearchIndex(item.Id);
        _avatarExplorerApp.SaveItemDatabase();

        EditTagsOverlay_Hide();
        Main_ReloadCurrentWindow();
    }
    #endregion
}
