using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.Sort;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class SelectAvatarsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public partial string Title { get; set; } = string.Empty;
    [Reactive] public partial bool AllowTempAvatarCreation { get; set; } = false;
    [Reactive] public partial IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    [Reactive] public partial string SearchText { get; set; } = string.Empty;
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
        var result = InstanceRepository.ItemGroupService.SearchItems(searchQuery, SearchResultTypes.All);
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
        var userPreference = InstanceRepository.UserPreferences;
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

        // BoothIdを指定するかどうか
        var setBoothId = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.SetBoothIdForTempAvatar]
        );

        var boothId = -1;
        var parseBoothIdFailed = false;
        if (setBoothId)
        {
            var boothIdInput = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.SetBoothIdForTempAvatar]);
            if (boothIdInput != null)
            {
                boothId = ValueParser.Int(BoothUtils.ExtractBoothIdFromUrl(boothIdInput), -1);
                if (boothId < 0) parseBoothIdFailed = true;
            }
        }

        if (parseBoothIdFailed)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance[Loc.Warning.InvalidBoothId],
                NotificationType.Warning
            );
        }

        InstanceRepository.TempAvatars.Create(newTempAvatarName, boothId);

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.CreateTempAvatar],
            NotificationType.Success
        );

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
