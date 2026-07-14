using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Services.System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditTagsViewModel : ViewModelBase
{
    [Reactive] public ObservableCollection<string> Tags { get; set; } = []; // 変更時、もしくはLocalizerの言語変更時にテキストを更新する
    private TaskCompletionSource<string[]?> _tcs = new();

    [Reactive] public string NewTag { get; set; } = string.Empty;
    public IReactiveCommand CreateTagCommand { get; }
    public IReactiveCommand ClearNewTagCommand { get; }

    [Reactive] public int SelectedIndex { get; set; } = -1;
    [Reactive] public IEnumerable<string> ExistTags { get; set; } = [];

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public EditTagsViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Tags.ToArray()));
        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));

        CreateTagCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                if (!string.IsNullOrEmpty(NewTag) && !Tags.Contains(NewTag))
                {
                    Tags.Add(NewTag);
                    NewTag = string.Empty;
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }, outputScheduler: RxSchedulers.MainThreadScheduler);
        ClearNewTagCommand = ReactiveCommand.Create(() => NewTag = string.Empty);

        

        this.WhenAnyValue(i => i.SelectedIndex)
            .Subscribe(i =>
            {
                if (i >= 0 && i < ExistTags.Count())
                {
                    var selectedTag = ExistTags.ElementAt(i);
                    if (!Tags.Contains(selectedTag))
                        Tags.Add(selectedTag);

                    SelectedIndex = -1;
                }
            });
    }

    public void Open(IEnumerable<string>? tags = null)
    {
        if (tags != null) Tags = new ObservableCollection<string>(tags);
    }

    public void RefleshTags()
    {
        ExistTags = AvatarExplorerApp.Instance.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Distinct();
    }

    public Task<string[]?> WaitForResult()
    {
        _tcs = new TaskCompletionSource<string[]?>();
        return _tcs.Task;
    }
}
