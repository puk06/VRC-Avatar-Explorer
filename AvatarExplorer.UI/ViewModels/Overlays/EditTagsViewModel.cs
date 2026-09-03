using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditTagsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public ObservableCollection<string> Tags { get; set; } = [];
    private TaskCompletionSource<string[]?> _tcs = new();

    [Reactive] public string NewTag { get; set; } = string.Empty;
    public IReactiveCommand CreateTagCommand { get; }
    public IReactiveCommand ClearNewTagCommand { get; }

    [Reactive] public int SelectedIndex { get; set; } = -1;

    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<string> ExistTags { get; set; } = [];
    private List<string> _allExistTags = [];

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public EditTagsViewModel()
    {
        CreateTagCommand = ReactiveCommand.Create(CreateTag);
        ClearNewTagCommand = ReactiveCommand.Create(ClearNewTagField);
        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(_ => ApplySearchFilter());
    }

    public Task<string[]?> ShowAsync(IEnumerable<string>? tags = null)
    {
        RefleshTags();

        Tags = new ObservableCollection<string>(tags ?? []);
        NewTag = string.Empty;
        SearchText = string.Empty;

        _tcs = new();

        return _tcs.Task;
    }

    public void RefleshTags()
    {
        _allExistTags = InstanceRepository.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Distinct()
            .ToList();
        ApplySearchFilter();
    }

    public void CreateTag()
    {
        if (!string.IsNullOrEmpty(NewTag) && !Tags.Contains(NewTag))
        {
            Tags.Add(NewTag);
            NewTag = string.Empty;
        }
    }

    private void ApplySearchFilter()
    {
        ExistTags = string.IsNullOrWhiteSpace(SearchText)
            ? _allExistTags
            : _allExistTags.Where(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void OnTagClick(string tag) => Tags.Remove(tag);

    public void OnExistTagSelectionChanged()
    {
        if (ExistTags.IsValidIndex(SelectedIndex))
        {
            var selectedTag = ExistTags.ElementAt(SelectedIndex);
            if (!Tags.Contains(selectedTag))
                Tags.Add(selectedTag);
        }

        SelectedIndex = -1;
    }

    public void Confirm() => _tcs.SetResult(Tags.ToArray());
    public void Cancel() => _tcs.SetResult(null);
    public void ClearNewTagField() => NewTag = string.Empty;
}
