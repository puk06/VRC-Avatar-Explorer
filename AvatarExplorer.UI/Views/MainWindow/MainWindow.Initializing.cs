using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.System;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void InitializeTitle()
    {
        Title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);
    }
    private void CheckAdministratorMode()
    {
        try
        {
            if (ProcessUtils.IsWindows() && SchemeService.IsRunAsAdmin())
            {
                Title = string.Format("VRC Avatar Explorer v{0} - [{1}]", AvatarExplorerApp.CurrentVersion, Localizer.Instance[LocalizationKey.Title.AdministratorMode]);
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Warning.Default], Localizer.Instance[LocalizationKey.Warning.RunningInAdministratorMode]);
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to determine whether the process is running with administrator privileges.", ex);
        }
    }
    private void InitializeAvatarExplorer()
    {
        try
        {
            _avatarExplorerApp.LoadItemDatabase();
            _avatarExplorerApp.LoadCommonAvatarDatabase();
            _avatarExplorerApp.LoadBulkImportPresetDatabase();
            _avatarExplorerApp.LoadRuntimeSettings();
            _avatarExplorerApp.StartAutoBackup();
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to initialize Avatar Explorer.", ex);
        }
    }
    private void InitializeUserPreferences()
    {
        _userPreferences = UserPreferencesService.Load(SystemPath.UserPreferencesFilePath);
    }
    private void InitializeContextMenuHandlers()
    {
        _contextMenuHandlers = new()
        {
            { ActionKey.OpenItemFolder, ItemButton_ContextMenu_OpenItemFolder },
            { ActionKey.CopyBoothLink, ItemButton_ContextMenu_CopyBoothLink },
            { ActionKey.OpenBoothLink, ItemButton_ContextMenu_OpenBoothLink },
            { ActionKey.ShowOtherItemsByAuthor, ItemButton_ContextMenu_ShowOtherItemsByAuthor },
            { ActionKey.ChangeThumbnail, ItemButton_ContextMenu_ChangeThumbnail },
            { ActionKey.FetchThumbnail, ItemButton_ContextMenu_FetchThumbnail },
            { ActionKey.EditItem, ItemButton_ContextMenu_EditItem },
            { ActionKey.EditItemTitle, ItemButton_ContextMenu_EditItemTitle },
            { ActionKey.EditItemMemo, ItemButton_ContextMenu_EditMemo },
            { ActionKey.AddToBulkImportList, ItemButton_ContextMenu_AddToBulkImportList },
            { ActionKey.AddItemFile, ItemButton_ContextMenu_AddItemFile },
            { ActionKey.AddItemFolder, ItemButton_ContextMenu_AddItemFolder },
            { ActionKey.EditImplementedAvatar, ItemButton_ContextMenu_EditImplementedAvatar },
            { ActionKey.EditItemTag, ItemButton_ContextMenu_EditItemTag },
            { ActionKey.RemoveItem, ItemButton_ContextMenu_RemoveItem },
            { ActionKey.OpenFile, ItemButton_ContextMenu_OpenFile },
            { ActionKey.AddFileToBulkImportList, ItemButton_ContextMenu_AddFileToBulkImportList },
            { ActionKey.OpenFileInExplorer, ItemButton_ContextMenu_OpenFileInExplorer },
            { ActionKey.RemovePreset, ItemButton_ContextMenu_RemovePreset }
        };
    }
    private void InitializeLanguageBox()
    {
        Localizer.Instance.LoadFromFolder("locales");

        string[] languages = Localizer.Instance.GetLanguageList();

        SettingsOverlay_LanguageComboBox.Items.Clear();
        SettingsOverlay_LanguageComboBox.Items.AddRange(languages);

        InitialSetupOverlay_LanguageComboBox.Items.Clear();
        InitialSetupOverlay_LanguageComboBox.Items.AddRange(languages);
    }

    private void InitializePipeServer()
    {
        SingleInstanceService.OnPipeMessageReceived += OnPipeMessageReceived;
        SingleInstanceService.StartServer();
    }
    private void OnPipeMessageReceived(string[] args)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            Activate();
            await SetApplicationArgs(args);
        });
    }

    private async Task CheckSchemeAsync()
    {
        try
        {
            void skipScheme()
            {
                SchemeService.MarkSchemeSkipped();
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Scheme.RegisterSkipped]);
            }

            if (SchemeService.IsSchemeRegistered())
            {
                string? currentInternalSchemePath = SchemeService.GetInternalSchemePath();

                if (!string.IsNullOrEmpty(currentInternalSchemePath) && !SchemeService.IsSkipped(currentInternalSchemePath) && currentInternalSchemePath != ProcessUtils.GetCurrentProcessPath())
                {
                    YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Scheme.PathChanged]);
                    if (result == null) return;
                    
                    if (result == YesNoResult.Yes) await Main_RegisterSchemeAsync();
                    else skipScheme();
                }
                else if (string.IsNullOrEmpty(currentInternalSchemePath))
                {
                    YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Scheme.RegisterAgain]);
                    if (result == null) return;
                    
                    if (result == YesNoResult.Yes) await Main_RegisterSchemeAsync();
                    else skipScheme();
                }
            }
            else
            {
                YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Scheme.Register]);
                if (result == null) return;
                
                if (result == YesNoResult.Yes) await Main_RegisterSchemeAsync();
                else skipScheme();
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("The scheme data could not be fully validated due to an internal error.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CheckSchemeFailed]);
        }
    }

    private async Task Main_RegisterSchemeAsync()
    {
        try
        {
            if (!SchemeService.IsRunAsAdmin())
            {
                YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Scheme.RestartAsAdmin]);
                if (result != null && result == YesNoResult.Yes) SchemeService.RestartAsAdmin();
            }
            else
            {
                SchemeService.RegisterScheme();
                DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Scheme.RegisterSuccess]);
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to register scheme.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RegisterSchemeFailed]);
        }
    }
}
