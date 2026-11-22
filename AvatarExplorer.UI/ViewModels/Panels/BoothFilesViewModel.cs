using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.System;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Panels;

public partial class BoothFilesViewModel : ViewModelBase, IInitializable
{
    [Reactive] public partial bool HasBoothItem { get; private set; }
    [Reactive] public partial bool HasCache { get; private set; }
    [Reactive] public partial bool IsFetching { get; private set; }
    [Reactive] public partial IReadOnlyList<BoothFilesVariation> Variations { get; private set; } = [];
    [Reactive] public partial BoothFilesVariation? SelectedVariation { get; set; }
    [Reactive] public partial bool ShowVariationPicker { get; private set; }

    public IReadOnlyList<BoothFilesEntry> SelectedFiles => SelectedVariation?.Files ?? [];
    public bool HasSelectedFiles => SelectedVariation is { HasFiles: true };

    public IReactiveCommand FetchCommand { get; }
    public IReactiveCommand OpenBoothLinkCommand { get; }

    private Item? _currentItem;

    public BoothFilesViewModel()
    {
        FetchCommand = ReactiveCommand.CreateFromTask(Fetch, this.WhenAnyValue(i => i.HasBoothItem));
        OpenBoothLinkCommand = ReactiveCommand.Create(OpenBoothLink, this.WhenAnyValue(i => i.HasBoothItem));

        InitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SelectedVariation)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SelectedFiles));
                this.RaisePropertyChanged(nameof(HasSelectedFiles));
            });

        InstanceRepository.NavigationService.SelectionChanged += RefreshCurrentItem;
        InstanceRepository.Items.OnUpdated += RefreshCurrentItem;
    }

    private void RefreshCurrentItem()
    {
        SetCurrentItem(InstanceRepository.NavigationService.GetCurrentItemId());
    }

    public void SetCurrentItem(string? itemId)
    {
        var selectedId = _currentItem?.Identifier == itemId ? SelectedVariation?.Id : null;
        var item = itemId == null ? null : InstanceRepository.Items.Get(itemId);

        _currentItem = item is { BoothId: >= 0 } ? item : null;
        HasBoothItem = _currentItem != null;

        var boothId = _currentItem?.BoothId.ToString();
        var cache = boothId == null ? null : InstanceRepository.VariationHashes.GetAll().FirstOrDefault(v => v.ItemId == boothId);

        HasCache = cache != null;
        Variations = cache != null && itemId != null ? GetVariations(cache, itemId) : [];
        SelectedVariation = Variations.FirstOrDefault(v => v.Id == selectedId) ?? (Variations.Count > 0 ? Variations[0] : null);
        ShowVariationPicker = Variations.Count > 1;
    }

    private static BoothFilesVariation[] GetVariations(VariationHash cache, string itemId)
    {
        if (cache.VariationInfos.Count == 0) return [];

        var files = InstanceRepository.Items.EnumerateItemFiles(itemId);
        var fileNames = files.Select(file => file.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // インポート時の展開先には、元ファイルの拡張子を除いた名前が使われる。
        var folderNames = files.Select(file => file.ParentFolderName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return cache.VariationInfos
            .OrderBy(v => v.Key)
            .Select(v =>
                new BoothFilesVariation(
                    v.Key,
                    string.IsNullOrEmpty(v.Value.Name) ? v.Key : v.Value.Name,
                    v.Value.Files.Select(file =>
                        new BoothFilesEntry(
                            file.FileName,
                            !fileNames.Contains(file.FileName) && !folderNames.Contains(Path.GetFileNameWithoutExtension(file.FileName))
                        )
                    ).ToArray()
                )
            ).ToArray();
    }

    private async Task Fetch()
    {
        if (_currentItem is not { BoothId: >= 0 } item) return;

        var boothId = item.BoothId.ToString();
        var hadCache = HasCache;

        IsFetching = true;

        try
        {
            var updates = await InstanceRepository.VariationHashes.CheckVariationAndNotify(boothId);
            if (updates == null)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.RetrieveItemFailed],
                    NotificationType.Error
                );
                return;
            }

            if (hadCache && updates.Count > 0) VariationUpdateNotification.Show(updates);
            if (_currentItem is { } current && current.BoothId.ToString() == boothId)
                SetCurrentItem(current.Identifier);
        }
        finally
        {
            IsFetching = false;
        }
    }

    private void OpenBoothLink()
    {
        if (_currentItem == null) return;
        ContextMenuHandlerService.Handle(ActionKey.OpenBoothLink, _currentItem.Identifier);
    }
}

public sealed record BoothFilesVariation(string Id, string Name, IReadOnlyList<BoothFilesEntry> Files)
{
    public bool HasFiles => Files.Count > 0;
}
public sealed record BoothFilesEntry(string Name, bool IsNotAdded);
