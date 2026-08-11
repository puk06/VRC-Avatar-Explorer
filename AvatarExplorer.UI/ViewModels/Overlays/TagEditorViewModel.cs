using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class TagEditorViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public IEnumerable<string> ExistTags { get; set; } = [];
    [Reactive] public int SelectedTagIndex { get; set; } = -1;
    [Reactive] public string NewTagName { get; set; } = string.Empty;

    public IReactiveCommand RenameCommand { get; }
    public IReactiveCommand RemoveCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    private static ItemRepository Items => AvatarExplorerApp.Instance.Items;

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
    }

    public void Open()
    {
        RefleshExistTags();
        SelectedTagIndex = 0;
        IsVisible = true;
    }

    private void RefleshExistTags()
    {
        ExistTags = AvatarExplorerApp.Instance.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Distinct();
    }

    public void Close()
    {
        SelectedTagIndex = -1;
        IsVisible = false;
    }

    public async Task Rename()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= ExistTags.Count()) return;

        var sourceTagName = ExistTags.ElementAt(SelectedTagIndex);
        var targetTagName = NewTagName;

        if (sourceTagName == targetTagName) return;

        if (IsTagNameExist(targetTagName))
        {
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RenameTagAlreadyExist, targetTagName)
            );
            if (!result) return;
        }
        else
        {
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Dialog.Confirmation.RenameTag, [sourceTagName, targetTagName])
            );
            if (!result) return;
        }

        Items.RenameTag(sourceTagName, targetTagName);

        var previousSelectedIndex = SelectedTagIndex;
        SelectedTagIndex = -1;
        RefleshExistTags();
        SelectedTagIndex = previousSelectedIndex;
    }

    public async Task Remove()
    {
        if (SelectedTagIndex < 0 || SelectedTagIndex >= ExistTags.Count()) return;

        var sourceTagName = ExistTags.ElementAt(SelectedTagIndex);

        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.RemoveTag, sourceTagName)
        );
        if (!result) return;

        Items.RemoveTag(sourceTagName);

        RefleshExistTags();
        SelectedTagIndex = -1;
    }

    private static bool IsTagNameExist(string tagName)
    {
        return AvatarExplorerApp.Instance.Items.GetAll()
            .SelectMany(i => i.Tags)
            .Contains(tagName);
    }
}
