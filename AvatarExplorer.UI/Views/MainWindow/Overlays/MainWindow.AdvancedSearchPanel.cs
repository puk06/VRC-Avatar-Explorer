using System.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void AdvancedSearchPanel_ApplyValues(SearchFilter searchFilter)
    {
        searchFilter.IsOrCondition = AdvancedSearchPanel_OrSearch.IsChecked ?? false;
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Title.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.Title, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Author.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.Author, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Booth.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.BoothId, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Avatar.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.SupportedAvatar, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Category.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.Category, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Memo.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.ItemMemo, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Folder.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.FolderName, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_File.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.FileName, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Implemented.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.ImplementedAvatar, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_NotImplemented.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.NotImplementedAvatar, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_Tag.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.Tag, value)));
        searchFilter.SearchTokens.AddRange(TextParser.Parse(AdvancedSearchPanel_CommonAvatar.Text ?? string.Empty).Select(value => new SearchToken(SearchTokenType.CommonAvatar, value)));
    }
}
