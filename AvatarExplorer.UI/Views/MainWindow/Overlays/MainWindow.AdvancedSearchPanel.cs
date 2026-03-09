using System.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Utils;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void AdvancedSearchPanel_ApplyValues(SearchFilter searchFilter)
    {
        searchFilter.IsOrSearch = AdvancedSearchPanel_OrSearch.IsChecked ?? false;
        searchFilter.Titles.AddRange(TextParser.Parse(AdvancedSearchPanel_Title.Text ?? string.Empty));
        searchFilter.Authors.AddRange(TextParser.Parse(AdvancedSearchPanel_Author.Text ?? string.Empty));
        searchFilter.BoothIds.AddRange(TextParser.Parse(AdvancedSearchPanel_Booth.Text ?? string.Empty));
        searchFilter.SupportedAvatars.AddRange(TextParser.Parse(AdvancedSearchPanel_Avatar.Text ?? string.Empty));
        searchFilter.Categories.AddRange(TextParser.Parse(AdvancedSearchPanel_Category.Text ?? string.Empty).Select(SearchUtils.ParseCategory));
        searchFilter.ItemMemos.AddRange(TextParser.Parse(AdvancedSearchPanel_Memo.Text ?? string.Empty));
        searchFilter.FolderNames.AddRange(TextParser.Parse(AdvancedSearchPanel_Folder.Text ?? string.Empty));
        searchFilter.FileNames.AddRange(TextParser.Parse(AdvancedSearchPanel_File.Text ?? string.Empty));
        searchFilter.ImplementedAvatars.AddRange(TextParser.Parse(AdvancedSearchPanel_Implemented.Text ?? string.Empty));
        searchFilter.NotImplementedAvatars.AddRange(TextParser.Parse(AdvancedSearchPanel_NotImplemented.Text ?? string.Empty));
        searchFilter.Tags.AddRange(TextParser.Parse(AdvancedSearchPanel_Tag.Text ?? string.Empty));
        searchFilter.CommonAvatars.AddRange(TextParser.Parse(AdvancedSearchPanel_CommonAvatar.Text ?? string.Empty));
    }
}
