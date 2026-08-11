using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Interfaces;
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
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Tags.ToArray()));
        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));

        CreateTagCommand = ReactiveCommand.Create(CreateTag);
        ClearNewTagCommand = ReactiveCommand.Create(() => NewTag = string.Empty);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(_ => ApplySearchFilter());
    }

    public void RefleshTags()
    {
        _allExistTags = AvatarExplorerApp.Instance.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Distinct()
            .ToList();
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        ExistTags = string.IsNullOrWhiteSpace(SearchText)
            ? _allExistTags
            : _allExistTags.Where(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
    }
    
    public void CreateTag()
    {
        if (!string.IsNullOrEmpty(NewTag) && !Tags.Contains(NewTag))
        {
            Tags.Add(NewTag);
            NewTag = string.Empty;
        }
    }

    public void OnTagClick(string tag) => Tags.Remove(tag);

    public void OnExistTagSelectionChanged()
    {
        if (SelectedIndex >= 0 && SelectedIndex < ExistTags.Count())
        {
            var selectedTag = ExistTags.ElementAt(SelectedIndex);
            if (!Tags.Contains(selectedTag))
                Tags.Add(selectedTag);
        }

        SelectedIndex = -1;
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
}
