using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.Sort;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class SelectAvatarsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public bool AllowTempAvatarCreation { get; set; } = false;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    [Reactive] public string SearchText { get; set; } = string.Empty;
    private TaskCompletionSource<string[]?> _tcs = new();
    private List<ItemViewModel> _allAvatars = [];

    public IReactiveCommand SelectItemCommand { get; }
    public IReactiveCommand SelectVisibleCommand { get; }
    public IReactiveCommand UnselectVisibleCommand { get; }

    public IReactiveCommand AddTempAvatarCommand { get; }
    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    private bool IncludeCommonAvatar = false;
    private bool IncludeTempAvatar = true;

    public SelectAvatarsViewModel()
    {
        AddTempAvatarCommand = ReactiveCommand.Create(AddTempAvatar);
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(SelectItem);
        SelectVisibleCommand = ReactiveCommand.Create(() => SetVisibleStatus(true));
        UnselectVisibleCommand = ReactiveCommand.Create(() => SetVisibleStatus(false));

        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Close);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SearchText)
            .Subscribe(ApplySearchResult);
    }

    public Task<string[]?> ShowAsync(string title, string[]? avatars = null, bool includeCommonAvatar = false, bool includeTempAvatar = true, bool allowCreateTempAvatar = false)
    {
        Title = title;
        IncludeCommonAvatar = includeCommonAvatar;
        IncludeTempAvatar = includeTempAvatar;
        AllowTempAvatarCreation = allowCreateTempAvatar;
        SearchText = string.Empty;

        RefleshAvatars(IncludeCommonAvatar, IncludeTempAvatar);

        if (avatars != null)
            _allAvatars.ForEach(i => i.IsSelected = avatars.Contains(i.Identifier));

        _tcs = new();
        return _tcs.Task;
    }

    private void SelectItem(ItemViewModel item) => item.IsSelected = !item.IsSelected;
    private void SetVisibleStatus(bool status) => Avatars.ForEach(i => i.IsSelected = status);

    private void ApplySearchResult(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Avatars = _allAvatars;
            return;
        }

        var searchQuery = searchText + " OR=true";
        var result = InstanceRepository.ItemGroupService.SearchItems(searchQuery, SearchResultType.All);
        if (result == null)
        {
            Avatars = _allAvatars;
            return;
        }

        Avatars = _allAvatars.Where(i => result.Contains(i.Identifier)).ToList();
    }

    private void RefleshAvatars(bool includeCommonAvatar, bool includeTempAvatar)
    {
        var avatars = InstanceRepository.ItemGroupService.GetAvatars(includeCommonAvatar, includeTempAvatar, rawIdentifier: true);
        var userPreference = InstanceRepository.UserPreferences.Settings;
        var sortedAvatars = ItemSortService.SortAvatars(
            avatars,
            userPreference.SortOrder,
            userPreference.SortDirection,
            userPreference.RemoveBrackets,
            rawIdentifier: true
        );

        _allAvatars = sortedAvatars
            .Select(NavigationItemFactory.CreateFromNavigationable)
            .Select(i => i.Update())
            .ToList();

        Avatars = _allAvatars;
    }

    private async Task AddTempAvatar()
    {
        var newTempAvatarName = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.NewTempAvatarName]);
        if (string.IsNullOrEmpty(newTempAvatarName)) return;

        InstanceRepository.TempAvatars.Create(newTempAvatarName);
        RefleshAvatars(IncludeCommonAvatar, IncludeTempAvatar);
        ApplySearchResult(SearchText);
    }

    private void Confirm()
    {
        _tcs.SetResult(
            Avatars
                .Where(i => i.IsSelected)
                .Select(i => i.Identifier)
                .ToArray()
        );
    }
    private void Close() => _tcs.SetResult(null);
}
