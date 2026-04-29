using System.Text;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.IO;

internal static class DataExporter
{
    internal static async Task<ErrorOr<Success>> Export(ExportContext exportContext, ExportRequest exportRequest)
    {
        return exportRequest.ExportType switch
        {
            DataExportType.Csv => await ToCsv(exportContext, exportRequest),
            _ => Error.Unexpected(description: $"Unexpected export type: {exportRequest.ExportType}")
        };
    }
    
    private static async Task<ErrorOr<Success>> ToCsv(ExportContext exportContext, ExportRequest exportRequest)
    {
        try
        {
            Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(exportContext.Items.Where(i => i.Type == ItemType.Avatar), exportContext.TempAvatars);

            FileSystemService.PrepareFileDirectory(exportRequest.FilePath);
            using StreamWriter sw = new(exportRequest.FilePath, false, Encoding.UTF8);
            await sw.WriteLineAsync("Id,Title,AuthorName,ImagePath,Category,Memo,SupportedAvatars,ImplementedAvatars,BoothId,ItemPath,Tags");

            foreach (Item item in exportContext.Items)
            {
                List<string> supportedAvatarNames = new();
                foreach (string supportedAvatarId in AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatarsView, exportContext.CommonAvatars, exportRequest.IncludeCommonToSupported))
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
                string imagePath = CsvUtils.EscapeCsv(item.ThumbnailFileName);

                string categoryName;
                if (item.Type == ItemType.Custom) categoryName = item.CustomCategory;
                else categoryName = exportContext.LocalizedItemTypesMapping.TryGetValue(item.Type, out string? value) ? value : item.Type.ToString();

                string category = CsvUtils.EscapeCsv(categoryName);
                string memo = CsvUtils.EscapeCsv(item.ItemMemo);
                string supportedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, supportedAvatarNames));
                string implementedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, implementedAvatarNames));
                string boothId = CsvUtils.EscapeCsv(item.BoothId.ToString());
                string itemPath = CsvUtils.EscapeCsv(ItemUtils.GetItemPath(exportContext.RuntimeSettings.DataRootDirectory, item.ItemPath));
                string tags = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.TagsView));

                await sw.WriteLineAsync($"{itemId},{itemTitle},{authorName},{imagePath},{category},{memo},{supportedAvatarsList},{implementedAvatarsList},{boothId},{itemPath},{tags}");
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
