using System.Text;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

internal static class DataExporter
{
    internal static async Task<ErrorOr<Success>> Export(DataExportType exportType, IEnumerable<Item> items, IEnumerable<CommonAvatar> commonAvatars, IEnumerable<TempAvatar> tempAvatars, Dictionary<ItemType, string> localizedItemTypesMapping, RuntimeSettings runtimeSettings, string filePath, bool includeCommonToSupported)
    {
        return exportType switch
        {
            DataExportType.Csv => await ToCsv(items, commonAvatars, tempAvatars, localizedItemTypesMapping, runtimeSettings, filePath, includeCommonToSupported),
            _ => Error.Unexpected(description: $"Unexpected export type: {exportType}")
        };
    }
    
    private static async Task<ErrorOr<Success>> ToCsv(IEnumerable<Item> items, IEnumerable<CommonAvatar> commonAvatars, IEnumerable<TempAvatar> tempAvatars, Dictionary<ItemType, string> localizedItemTypesMapping, RuntimeSettings runtimeSettings, string filePath, bool includeCommonToSupported)
    {
        try
        {
            Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(items.Where(i => i.Type == ItemType.Avatar), tempAvatars);

            FileSystemService.PrepareFileDirectory(filePath);
            using StreamWriter sw = new(filePath, false, Encoding.UTF8);
            await sw.WriteLineAsync("Id,Title,AuthorName,AuthorImageFilePath,ImagePath,Category,Memo,SupportedAvatars,ImplementedAvatars,BoothId,ItemPath,Tags");

            foreach (Item item in items)
            {
                List<string> supportedAvatarNames = new();
                foreach (string supportedAvatarId in AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, commonAvatars, includeCommonToSupported))
                {
                    string avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, supportedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    supportedAvatarNames.Add(avatarTitle);
                }

                List<string> implementedAvatarNames = new();
                foreach (string implementedAvatarId in item.ImplementedAvatarsView.Distinct())
                {
                    string avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, implementedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    implementedAvatarNames.Add(avatarTitle);
                }

                string itemId = CsvUtils.EscapeCsv(item.Id);
                string itemTitle = CsvUtils.EscapeCsv(item.Title);
                string authorName = CsvUtils.EscapeCsv(item.Author);
                string authorImageFilePath = CsvUtils.EscapeCsv(item.AuthorThumbnmailFileName);
                string imagePath = CsvUtils.EscapeCsv(item.ThumbnmailFileName);

                string categoryName;
                if (item.Type == ItemType.Custom) categoryName = item.CustomCategory;
                else categoryName = localizedItemTypesMapping.TryGetValue(item.Type, out string? value) ? value : item.Type.ToString();

                string category = CsvUtils.EscapeCsv(categoryName);
                string memo = CsvUtils.EscapeCsv(item.ItemMemo);
                string supportedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, supportedAvatarNames));
                string implementedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, implementedAvatarNames));
                string boothId = CsvUtils.EscapeCsv(item.BoothId.ToString());
                string itemPath = CsvUtils.EscapeCsv(ItemUtils.GetItemPath(runtimeSettings.DataRootDirectory, item.ItemPath));
                string tags = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.TagsView));

                await sw.WriteLineAsync($"{itemId},{itemTitle},{authorName},{authorImageFilePath},{imagePath},{category},{memo},{supportedAvatarsList},{implementedAvatarsList},{boothId},{itemPath},{tags}");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to export to csv.", ex);
            return Error.Failure(description: "Failed to export to csv.");
        }
    }
}
