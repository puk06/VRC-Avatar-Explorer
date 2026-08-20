using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class BulkImportViewModel : ViewModelBase, IInitializable
{
    public event Action? OnItemsAdded;
    [Reactive] public ObservableCollection<BulkImportItemViewModel> Items { get; set; } = [];

    public IReactiveCommand CopyCommand { get; }
    public IReactiveCommand RemoveCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand SaveCommand { get; }

    public BulkImportViewModel()
    {
        CopyCommand = ReactiveCommand.Create<BulkImportItemViewModel>(CopyItem);
        RemoveCommand = ReactiveCommand.Create<BulkImportItemViewModel>(RemoveItem);
        ImportCommand = ReactiveCommand.CreateFromTask(Import);
        ResetCommand = ReactiveCommand.Create(Reset);
        SaveCommand = ReactiveCommand.CreateFromTask(Save);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        InstanceRepository.Items.OnUpdated += RefreshItems;
        InstanceRepository.UserPreferencesRepository.OnSettingsChanged += _ => OnUserPreferencesChanged();
        Items.CollectionChanged += (s, e) =>
        {
            if (e.Action is NotifyCollectionChangedAction.Add)
                OnItemsAdded?.Invoke();
        };
    }

    private void OnUserPreferencesChanged()
    {
        var settings = InstanceRepository.UserPreferences;
        foreach (var item in Items)
        {
            item.Update(settings.NormalIconSize, settings.RemoveBrackets);
        }
    }

    private void CopyItem(BulkImportItemViewModel item) => Items.Add(item.Copy().Update());
    private void RemoveItem(BulkImportItemViewModel item) => Items.Remove(item);

    private async Task Import()
    {
        var itemPathCategoryEntries = new List<UnitypackageImportEntry>();

        foreach (var bulkImportItem in Items)
        {
            var item = InstanceRepository.Items.Get(bulkImportItem.ItemId);
            if (item == null)
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.ItemNotFound],
                    NotificationType.Warning
                );
                continue;
            }

            var selectedPath = bulkImportItem.SelectedUnitypackagePath;
            if (string.IsNullOrEmpty(selectedPath))
            {
                NotificationManager.Show(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.UnitypackageNotFound],
                    NotificationType.Warning
                );
                continue;
            }

            if (!itemPathCategoryEntries.Any(i => i.FilePath == selectedPath))
            {
                itemPathCategoryEntries.Add(new()
                {
                    CategoryDisplayName = UnitypackageService.GetCategoryDisplayName(item.Category),
                    FilePath = selectedPath
                });
            }
        }

        await UnitypackageService.ImportWithProgress(itemPathCategoryEntries, Loc.Error.BulkImportFailed);
    }

    private void Reset()
    {
        Items.Clear();
    }

    private async Task Save()
    {
        var presetName = await InstanceRepository.MainWindow.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.NewBulkImportPresetName]);
        if (string.IsNullOrEmpty(presetName)) return;

        InstanceRepository.BulkImportPresets.Create(
            presetName,
            Items
                .Select(i => new BulkImportItem(i.ItemId, i.SelectedUnitypackagePath))
                .ToArray()
        );
    }
    
    public void OnBulkImportDropped(string value, bool isFile = false)
    {
        if (isFile)
        {
            var currentItem = InstanceRepository.NavigationService.GetCurrentItemId();
            if (currentItem == null) return;

            AddItem(currentItem, value);
        }
        else
        {
            AddItem(value);
        }
    }

    public void AddItem(string itemid, string? filePath = null)
    {
        var item = InstanceRepository.Items.Get(itemid);
        if (item == null)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ItemNotFound],
                NotificationType.Warning
            );
            return;
        }

        var unitypackagePaths = UnitypackageService.GetUnitypackagePaths(item.GetFolderPaths());

        if (unitypackagePaths.Length == 0)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.UnitypackageNotFound],
                NotificationType.Warning
            );
            return;
        }

        var bulkVm = new BulkImportItemViewModel()
        {
            ImageFileName = item.ThumbnailFileName,
            TitleRaw = item.Title,
            DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]),
            UnitypackageFullPaths = unitypackagePaths.ToArray(),
            ItemId = itemid,
            SelectedUnitypackage = 0
        };

        if (!string.IsNullOrEmpty(filePath))
        {
            var index = unitypackagePaths.IndexOf(filePath);
            if (index != -1) bulkVm.SelectedUnitypackage = index;
        }

        var settings = InstanceRepository.UserPreferences;
        Items.Add(bulkVm.Update(settings.NormalIconSize, settings.RemoveBrackets));
    }

    private void RefreshItems()
    {
        var newItems = new List<BulkImportItemViewModel>();

        var settings = InstanceRepository.UserPreferences;
        foreach (var itemVm in Items)
        {
            var item = InstanceRepository.Items.Get(itemVm.ItemId);
            if (item == null) continue;

            itemVm.ImageFileName = item.ThumbnailFileName;
            itemVm.TitleRaw = item.Title;
            itemVm.DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]);

            newItems.Add(itemVm.Update(settings.NormalIconSize, settings.RemoveBrackets));
        }

        Items = new ObservableCollection<BulkImportItemViewModel>(newItems);
    }
}
