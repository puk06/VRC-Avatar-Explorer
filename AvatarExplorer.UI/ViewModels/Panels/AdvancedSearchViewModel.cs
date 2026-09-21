using System.Collections.ObjectModel;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Panels;

public partial class AdvancedSearchViewModel : ViewModelBase, IInitializable
{
    [Reactive] public partial string Title { get; set; } = string.Empty;
    [Reactive] public partial string Author { get; set; } = string.Empty;
    [Reactive] public partial string BoothId { get; set; } = string.Empty;
    [Reactive] public partial string SupportedAvatar { get; set; } = string.Empty;
    [Reactive] public partial ObservableCollection<ItemCategoryViewModel> Categories { get; set; } = [];
    [Reactive] public partial IEnumerable<ItemCategoryViewModel> FilteredCategories { get; set; } = [];
    [Reactive] public partial ObservableCollection<ItemCategoryViewModel> SelectedCategories { get; set; } = [];
    [Reactive] public partial int SelectedCategoryIndex { get; set; } = -1;
    [Reactive] public partial string CategorySearchText { get; set; } = string.Empty;
    [Reactive] public partial string NewCategory { get; set; } = string.Empty;
    [Reactive] public partial string Memo { get; set; } = string.Empty;
    [Reactive] public partial string ImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial string NotImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial ObservableCollection<string> SelectedTags { get; set; } = [];
    [Reactive] public partial IEnumerable<string> ExistingTags { get; set; } = [];
    [Reactive] public partial int SelectedTagIndex { get; set; } = -1;
    [Reactive] public partial string TagSearchText { get; set; } = string.Empty;
    [Reactive] public partial string NewTag { get; set; } = string.Empty;
    [Reactive] public partial string CommonAvatar { get; set; } = string.Empty;
    [Reactive] public partial bool IsOr { get; set; }
    [Reactive] public partial bool IncludeHidden { get; set; }

    public event Action? SearchPropertyChanged;
    public IReactiveCommand AddCategoryCommand { get; }
    public IReactiveCommand AddTagCommand { get; }

    public AdvancedSearchViewModel()
    {
        AddCategoryCommand = ReactiveCommand.Create(AddNewCategory);
        AddTagCommand = ReactiveCommand.Create(AddNewTag);
        SelectedCategories.CollectionChanged += (_, _) => SearchPropertyChanged?.Invoke();
        SelectedTags.CollectionChanged += (_, _) => SearchPropertyChanged?.Invoke();
        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyPropertyChanged()
            .Subscribe(_ => SearchPropertyChanged?.Invoke());

        this.WhenAnyValue(x => x.CategorySearchText)
            .Subscribe(_ => ApplyCategoryFilter());

        this.WhenAnyValue(x => x.TagSearchText)
            .Subscribe(_ => ApplyTagFilter());

        Localizer.Instance.LanguageChanged += ReloadSearchComponents;
        InstanceRepository.Items.OnUpdated += ReloadSearchComponents;

        RefreshCategories();
        RefreshTags();
    }
    private void ReloadSearchComponents()
    {
        RefreshCategories();
        RefreshTags();
    }

    public void RefreshCategories()
    {
        var categories = InstanceRepository.ItemGroupService
            .GetCategoryFolders(includeEmptyCategory: false, includeAllCategory: false)
            .Select(folder => ItemCategory.FromIdentifier(folder.Identifier))
            .Select(category => new ItemCategoryViewModel(category).Update())
            .ToList();

        Categories.Clear();
        Categories.AddRange(categories);
        ApplyCategoryFilter();
    }

    public void RefreshTags() => ApplyTagFilter();

    public void AddSelectedCategory()
    {
        if (!FilteredCategories.IsValidIndex(SelectedCategoryIndex)) return;
        var category = FilteredCategories.ElementAt(SelectedCategoryIndex);
        if (!SelectedCategories.Any(selected => selected.Category.Equals(category.Category)))
            SelectedCategories.Add(category);
        SelectedCategoryIndex = -1;
    }

    public void RemoveSelectedCategory(ItemCategoryViewModel category) => SelectedCategories.Remove(category);

    public void ToggleCategoryNegation(ItemCategoryViewModel category)
    {
        var index = SelectedCategories.IndexOf(category);
        if (index < 0) return;

        var isNegated = category.DisplayName.StartsWith('~');
        var newName = isNegated ? category.DisplayName[1..] : "~" + category.DisplayName;
        var toggled = new ItemCategoryViewModel(ItemCategory.Get(newName)).Update();
        SelectedCategories[index] = toggled;
    }

    public void AddNewCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategory)) return;

        var category = new ItemCategoryViewModel(ItemCategory.Get(NewCategory.Trim())).Update();
        if (!SelectedCategories.Any(selected => selected.Category.Equals(category.Category)))
            SelectedCategories.Add(category);
        NewCategory = string.Empty;
    }

    public void AddSelectedTag()
    {
        if (!ExistingTags.IsValidIndex(SelectedTagIndex)) return;
        var tag = ExistingTags.ElementAt(SelectedTagIndex);
        if (!SelectedTags.Contains(tag)) SelectedTags.Add(tag);
        SelectedTagIndex = -1;
    }

    public void RemoveSelectedTag(string tag) => SelectedTags.Remove(tag);

    public void ToggleTagNegation(string tag)
    {
        var index = SelectedTags.IndexOf(tag);
        if (index < 0) return;

        var toggled = tag.StartsWith('~') ? tag[1..] : "~" + tag;
        SelectedTags[index] = toggled;
    }

    public void AddNewTag()
    {
        var tag = NewTag.Trim();
        if (string.IsNullOrEmpty(tag) || SelectedTags.Contains(tag)) return;

        SelectedTags.Add(tag);
        NewTag = string.Empty;
    }

    private void ApplyCategoryFilter()
    {
        FilteredCategories = string.IsNullOrWhiteSpace(CategorySearchText)
            ? Categories
            : Categories.Where(category => category.DisplayName.Contains(CategorySearchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void ApplyTagFilter()
    {
        var tags = InstanceRepository.Items.GetAll().SelectMany(item => item.Tags).Distinct().OrderBy(tag => tag);
        ExistingTags = string.IsNullOrWhiteSpace(TagSearchText)
            ? tags.ToList()
            : tags.Where(tag => tag.Contains(TagSearchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
