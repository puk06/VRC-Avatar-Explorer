using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class TagEditorViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public IEnumerable<string> ExistTags { get; set; } = [];
    [Reactive] public int SelectedTagIndex { get; set; } = -1;
    [Reactive] public string NewTagName { get; set; } = string.Empty;
    [Reactive] public string SearchText { get; set; } = string.Empty;

    private List<string> _allExistTags = [];

    public IReactiveCommand RenameCommand { get; }
    public IReactiveCommand RemoveCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public TagEditorViewModel()
    {
        CloseCommand = ReactiveCommand.Create(Close);
        RenameCommand = ReactiveCommand.CreateFromTask(Rename);
        RemoveCommand = ReactiveCommand.CreateFromTask(Remove);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SelectedTagIndex)
            .Subscribe(i =>
            {
                if (i < 0 || i >= ExistTags.Count())
                {
                    NewTagName = string.Empty;
                    return;
                }

                NewTagName = ExistTags.ElementAt(i);
            });

        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(_ => ApplySearchFilter());
    }

    public void Open()
    {
        RefleshExistTags();
        SearchText = string.Empty;
        SelectedTagIndex = ExistTags.Any() ? 0 : -1;
        IsVisible = true;
    }

    private void RefleshExistTags()
    {
        _allExistTags = InstanceRepository.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Distinct()
            .ToList();
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allExistTags
            : _allExistTags.Where(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        var previousTagName = SelectedTagIndex >= 0 && SelectedTagIndex < ExistTags.Count()
            ? ExistTags.ElementAt(SelectedTagIndex)
            : null;

        ExistTags = filtered;

        SelectedTagIndex = previousTagName != null
            ? Math.Max(0, filtered.IndexOf(previousTagName))
            : (filtered.Count > 0 ? 0 : -1);
    }

    public async Task Rename()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= ExistTags.Count()) return;

        var sourceTagName = ExistTags.ElementAt(SelectedTagIndex);
        var targetTagName = NewTagName;

        if (sourceTagName == targetTagName) return;

        if (IsTagNameExist(targetTagName))
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RenameTagAlreadyExist, targetTagName)
            );
            if (!result) return;
        }
        else
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RenameTag, [sourceTagName, targetTagName])
            );
            if (!result) return;
        }

        InstanceRepository.Items.RenameTag(sourceTagName, targetTagName);

        SelectedTagIndex = -1;
        RefleshExistTags();
        SelectedTagIndex = ExistTags.ToList().IndexOf(targetTagName);
    }
    public async Task Remove()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= ExistTags.Count()) return;

        var sourceTagName = ExistTags.ElementAt(SelectedTagIndex);

        var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveTag, sourceTagName)
        );
        if (!result) return;

        InstanceRepository.Items.RemoveTag(sourceTagName);

        RefleshExistTags();
        SelectedTagIndex = -1;
    }

    private static bool IsTagNameExist(string tagName)
    {
        return InstanceRepository.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Contains(tagName);
    }

    public void Close()
    {
        SelectedTagIndex = -1;
        IsVisible = false;
    }
}
