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
    private string? _editTagsOverlay_selectedItemId = null;

    private void EditTagsOverlay_Open(string itemId, IEnumerable<string>? tags = null)
    {
        _editTagsOverlay_selectedItemId = itemId;
        EditTagsOverlay.IsVisible = true;
        EditTagsOverlay_TagTextBox.Text = string.Empty;
        EditTagsOverlay_Initialize(tags);
    }
    private void EditTagsOverlay_Close()
    {
        _editTagsOverlay_selectedItemId = null;
        EditTagsOverlay.IsVisible = false;
    }

    private void EditTagsOverlay_Initialize(IEnumerable<string>? tags = null)
    {
        _editTagsOverlay_selectedTags.Clear();
        if (tags != null) _editTagsOverlay_selectedTags.AddRange(tags);

        EditTagsOverlay_RefleshTagsList();
        EditTagsOverlay_DrawTags();
    }
    private void EditTagsOverlay_RefleshTagsList()
    {
        EditTagsOverlay_TagComboBox.Items.Clear();
        EditTagsOverlay_TagComboBox.Items.AddRange(
            AvatarExplorer.GetAllItems()
                .SelectMany(i => i.TagsView)
                .Distinct()
                .Select(i => new ComboBoxItem() { Content = i })
        );
    }
    private void EditTagsOverlay_DrawTags()
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
            EditTagsOverlay_DrawTags();
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

        EditTagsOverlay_DrawTags();
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
        EditTagsOverlay_DrawTags();

        EditTagsOverlay_TagComboBox.SelectedIndex = -1;
    }
    private void EditTagsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditTagsOverlay_Close();
    private void EditTagsOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = AvatarExplorer.GetItemById(_editTagsOverlay_selectedItemId);
        if (item == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.UpdateTags(_editTagsOverlay_selectedTags);
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.UpdateSearchIndex(item.Id);
        AvatarExplorer.SaveItemDatabase();

        EditTagsOverlay_Close();
        Main_ReloadCurrentWindow();
    }
    #endregion
}
