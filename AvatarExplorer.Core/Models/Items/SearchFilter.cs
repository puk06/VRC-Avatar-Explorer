namespace AvatarExplorer.Core.Models.Items;

public class SearchFilter
{
    public List<string> Titles { get; } = new List<string>();
    public List<string> Authors { get; } = new List<string>();
    public List<string> BoothIds { get; } = new List<string>();
    public List<string> SupportedAvatars { get; } = new List<string>();
    public List<string> Categories { get; } = new List<string>();
    public List<string> ItemMemos { get; } = new List<string>();
    public List<string> FolderNames { get; } = new List<string>();
    public List<string> FileNames { get; } = new List<string>();
    public List<string> ImplementedAvatars { get; } = new List<string>();
    public List<string> NotImplementedAvatars { get; } = new List<string>();
    public List<string> Tags { get; } = new List<string>();
    public List<string> CommonAvatars { get; } = new List<string>();
    public bool IsOrSearch { get; set; } = false;
    public bool BrokenItems { get; set; } = false;
    public List<string> SearchWords { get; } = new List<string>();

    public bool IsEmpty =>
        Titles.Count == 0 &&
        Authors.Count == 0 &&
        BoothIds.Count == 0 &&
        SupportedAvatars.Count == 0 &&
        Categories.Count == 0 &&
        ItemMemos.Count == 0 &&
        FolderNames.Count == 0 &&
        FileNames.Count == 0 &&
        ImplementedAvatars.Count == 0 &&
        NotImplementedAvatars.Count == 0 &&
        Tags.Count == 0 &&
        CommonAvatars.Count == 0 &&
        SearchWords.Count == 0 &&
        !BrokenItems;
}
