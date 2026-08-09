using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class BulkImportViewModel : ViewModelBase, IInitializable
{
    public event Action? OnItemsChanged;
    [Reactive] public ObservableCollection<BulkImportItemViewModel> Items { get; set; } = [];

    public IReactiveCommand CopyCommand { get; }
    public IReactiveCommand RemoveCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand SaveCommand { get; }

    public BulkImportViewModel()
    {
        CopyCommand = ReactiveCommand.Create<BulkImportItemViewModel>(i => Items.Add(i.Copy().Update()));
        RemoveCommand = ReactiveCommand.Create<BulkImportItemViewModel>(i => Items.Remove(i));
        ImportCommand = ReactiveCommand.CreateFromTask(Import);
        ResetCommand = ReactiveCommand.Create(Reset);
        SaveCommand = ReactiveCommand.CreateFromTask(Save);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        AvatarExplorerApp.Instance.Items.OnUpdated += RefreshItems;
        UserPreferencesService.Instance.Repository.OnSettingsChanged += _ => OnUserPreferencesChanged();
        Items.CollectionChanged += (s, e) => OnItemsChanged?.Invoke();
    }

    private void OnUserPreferencesChanged()
    {
        var settings = UserPreferencesService.Instance.Repository.Settings;
        foreach (var item in Items)
        {
            item.Update(settings.NormalIconSize, settings.RemoveBrackets);
        }
    }

    private async Task Import()
    {
        var itemPathCategoryEntries = new List<UnitypackageImportEntry>();

        foreach (var bulkImportItem in Items)
        {
            var item = AvatarExplorerApp.Instance.Items.Get(bulkImportItem.ItemId);
            if (item == null)
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.ItemNotFound],
                    NotificationType.Warning
                );
                continue;
            }

            var selectedPath = bulkImportItem.SelectedUnitypackagePath;
            if (string.IsNullOrEmpty(selectedPath))
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.UnitypackageNotFound],
                    NotificationType.Warning
                );
                continue;
            }

            if (!itemPathCategoryEntries.Any(i => i.FilePath == selectedPath))
            {
                var category = item.Category.IsLocalizable ? Localizer.Instance[item.Category.ToString()] : item.Category.ToString();
                itemPathCategoryEntries.Add(new()
                {
                    CategoryDisplayName = category,
                    FilePath = selectedPath
                });
            }
        }

        MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Unitypackage.Status.Preparing]);
        var importResult = await UnitypackageService.Import(
            itemPathCategoryEntries,
            onProgress: async (name, percent) =>
            {
                MainWindowViewModel.Instance.ProgressVM.Update(
                    Localizer.Instance.Get(name, percent.ToString()),
                    percent
                );
            }
        );
        MainWindowViewModel.Instance.ProgressVM.Close();

        if (!importResult.IsError && !string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
        {
            var result = await LauncherService.OpenFile(TopLevelProvider.Current, importResult.ModifiedUnitypackagePath);
            if (result.IsError)
            {
                MainWindowViewModel.ShowNotification(
                    Localizer.Instance[Loc.Error.Default],
                    Localizer.Instance[Loc.Error.OpenFileFailed],
                    NotificationType.Error
                );
            }
        }
        else
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.BulkImportFailed],
                NotificationType.Error
            );
        }
    }

    private void Reset()
    {
        Items.Clear();
    }

    private async Task Save()
    {
        var presetName = await MainWindowViewModel.Instance.ShowTextDialog(Localizer.Instance[Loc.Dialog.Title.NewBulkImportPresetName]);
        if (string.IsNullOrEmpty(presetName)) return;

        AvatarExplorerApp.Instance.BulkImportPresets.Create(
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
            var currentItem = AvatarExplorerApp.Instance.ItemNavigationService.GetCurrentItemId();
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
        var item = AvatarExplorerApp.Instance.Items.Get(itemid);
        if (item == null)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ItemNotFound],
                NotificationType.Warning
            );
            return;
        }

        var unitypackagePaths = UnitypackageService.GetUnitypackagePaths(item.GetFolderPaths());

        if (unitypackagePaths.Length == 0)
        {
            MainWindowViewModel.ShowNotification(
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

        var settings = UserPreferencesService.Instance.Repository.Settings;
        Items.Add(bulkVm.Update(settings.NormalIconSize, settings.RemoveBrackets));
    }

    private void RefreshItems()
    {
        var newItems = new List<BulkImportItemViewModel>();

        var settings = UserPreferencesService.Instance.Repository.Settings;
        foreach (var itemVm in Items)
        {
            var item = AvatarExplorerApp.Instance.Items.Get(itemVm.ItemId);
            if (item == null) continue;

            itemVm.ImageFileName = item.ThumbnailFileName;
            itemVm.TitleRaw = item.Title;
            itemVm.DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]);

            newItems.Add(itemVm.Update(settings.NormalIconSize, settings.RemoveBrackets));
        }

        Items = new ObservableCollection<BulkImportItemViewModel>(newItems);
    }
}
