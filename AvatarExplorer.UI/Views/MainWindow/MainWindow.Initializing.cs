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
    private void Main_InitializeTitle()
    {
        Title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);
    }
    private void Main_CheckAdministratorMode()
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
    private void Main_InitializeUserPreferences()
    {
        _userPreferences = UserPreferencesService.Load(SystemPath.UserPreferencesFilePath);
    }
    private void Main_InitializeContextMenuHandlers()
    {
        _main_contextMenuHandlers = new()
        {
            { ActionKey.OpenItemFolder, Main_ItemButton_ContextMenu_OpenItemFolder },
            { ActionKey.CopyBoothLink, Main_ItemButton_ContextMenu_CopyBoothLink },
            { ActionKey.OpenBoothLink, Main_ItemButton_ContextMenu_OpenBoothLink },
            { ActionKey.ShowOtherItemsByAuthor, Main_ItemButton_ContextMenu_ShowOtherItemsByAuthor },
            { ActionKey.ChangeThumbnail, Main_ItemButton_ContextMenu_ChangeThumbnail },
            { ActionKey.FetchThumbnail, Main_ItemButton_ContextMenu_FetchThumbnail },
            { ActionKey.EditItem, Main_ItemButton_ContextMenu_EditItem },
            { ActionKey.EditItemTitle, Main_ItemButton_ContextMenu_EditItemTitle },
            { ActionKey.EditItemMemo, Main_ItemButton_ContextMenu_EditMemo },
            { ActionKey.AddToBulkImportList, Main_ItemButton_ContextMenu_AddToBulkImportList },
            { ActionKey.AddItemFile, Main_ItemButton_ContextMenu_AddItemFile },
            { ActionKey.AddItemFolder, Main_ItemButton_ContextMenu_AddItemFolder },
            { ActionKey.EditImplementedAvatar, Main_ItemButton_ContextMenu_EditImplementedAvatar },
            { ActionKey.EditItemTag, Main_ItemButton_ContextMenu_EditItemTag },
            { ActionKey.RemoveItem, Main_ItemButton_ContextMenu_RemoveItem },
            { ActionKey.OpenFile, Main_ItemButton_ContextMenu_OpenFile },
            { ActionKey.AddFileToBulkImportList, Main_ItemButton_ContextMenu_AddFileToBulkImportList },
            { ActionKey.OpenFileInExplorer, Main_ItemButton_ContextMenu_OpenFileInExplorer },
            { ActionKey.RemovePreset, Main_ItemButton_ContextMenu_RemovePreset },
            { ActionKey.EditTempAvatarName, Main_ItemButton_ContextMenu_EditTempAvatarName },
            { ActionKey.ResolveTempAvatar, Main_ItemButton_ContextMenu_ResolveTempAvatar },
            { ActionKey.RemoveTempAvatar, Main_ItemButton_ContextMenu_RemoveTempAvatar }
        };
    }
    private void Main_InitializeLanguageBox()
    {
        Localizer.Instance.LoadFromFolder("locales");

        string[] languages = Localizer.Instance.GetLanguageList();

        SettingsOverlay_LanguageComboBox.Items.Clear();
        SettingsOverlay_LanguageComboBox.Items.AddRange(languages);

        InitialSetupOverlay_LanguageComboBox.Items.Clear();
        InitialSetupOverlay_LanguageComboBox.Items.AddRange(languages);
    }

    private void Main_InitializePipeServer()
    {
        SingleInstanceService.OnPipeMessageReceived += Main_OnPipeMessageReceived;
        SingleInstanceService.StartServer();
    }
    private void Main_OnPipeMessageReceived(string[] args)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            Topmost = true;
            Activate();
            await SetApplicationArgs(args);
            Topmost = false;
        });
    }

    private async Task Main_CheckSchemeAsync()
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
                    if (result == null || result != YesNoResult.Yes) return;
                    
                    await Main_RegisterSchemeAsync();
                }
                else if (string.IsNullOrEmpty(currentInternalSchemePath))
                {
                    YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Scheme.RegisterAgain]);
                    if (result == null || result != YesNoResult.Yes) return;
                    
                    await Main_RegisterSchemeAsync();
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
