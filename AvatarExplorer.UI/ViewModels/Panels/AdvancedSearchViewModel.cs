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
    [Reactive] public partial ObservableCollection<AdvancedSearchTagViewModel> SelectedCategories { get; set; } = [];
    [Reactive] public partial int SelectedCategoryIndex { get; set; } = -1;
    [Reactive] public partial string CategorySearchText { get; set; } = string.Empty;
    [Reactive] public partial string NewCategory { get; set; } = string.Empty;
    [Reactive] public partial string Memo { get; set; } = string.Empty;
    [Reactive] public partial string ImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial string NotImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial ObservableCollection<AdvancedSearchTagViewModel> SelectedTags { get; set; } = [];
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
        if (!SelectedCategories.Any(selected => selected.ValueRaw == category.Category.ToString()))
        {
            SelectedCategories.Add(new AdvancedSearchTagViewModel(category.Category.ToString()) { IsLocalizable = category.Category.IsLocalizable }.Update());
        }

        SelectedCategoryIndex = -1;
    }

    public void RemoveSelectedCategory(AdvancedSearchTagViewModel category) => SelectedCategories.Remove(category);

    public void ToggleCategoryNegation(AdvancedSearchTagViewModel category)
    {
        var index = SelectedCategories.IndexOf(category);
        if (index < 0) return;

        SelectedCategories[index].IsNegation = !SelectedCategories[index].IsNegation;
        SearchPropertyChanged?.Invoke();
    }

    public void AddNewCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategory)) return;

        var category = new AdvancedSearchTagViewModel(NewCategory.Trim()).Update();
        if (!SelectedCategories.Any(selected => selected.ValueRaw == category.ValueRaw))
        {
            SelectedCategories.Add(category);
        }

        NewCategory = string.Empty;
    }

    public void AddSelectedTag()
    {
        if (!ExistingTags.IsValidIndex(SelectedTagIndex)) return;

        var tag = ExistingTags.ElementAt(SelectedTagIndex);
        if (!SelectedTags.Any(i => i.ValueRaw == tag))
        {
            SelectedTags.Add(new AdvancedSearchTagViewModel(tag).Update());
        }
    
        SelectedTagIndex = -1;
    }

    public void RemoveSelectedTag(AdvancedSearchTagViewModel tag) => SelectedTags.Remove(tag);

    public void ToggleTagNegation(AdvancedSearchTagViewModel tag)
    {
        var index = SelectedTags.IndexOf(tag);
        if (index < 0) return;

        SelectedTags[index].IsNegation = !SelectedTags[index].IsNegation;
        SearchPropertyChanged?.Invoke();
    }

    public void AddNewTag()
    {
        var tag = NewTag.Trim();
        if (string.IsNullOrEmpty(tag) || SelectedTags.Any(t => t.ValueRaw == tag)) return;

        SelectedTags.Add(new AdvancedSearchTagViewModel(tag).Update());
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
