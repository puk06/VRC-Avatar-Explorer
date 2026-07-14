using System.Text;
using AvatarExplorer.Core.Extensions;
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
            var avatarTitleMaps = ItemUtils.GetItemTitleMaps(exportContext.Items.Where(i => i.Type == ItemType.Avatar), exportContext.TempAvatars);

            FileSystemService.PrepareFileDirectory(exportRequest.FilePath);
            using StreamWriter sw = new(exportRequest.FilePath, false, Encoding.UTF8);
            await sw.WriteLineAsync("Id,Title,AuthorName,ImagePath,Category,Memo,SupportedAvatars,ImplementedAvatars,BoothId,ItemPath,Tags");

            foreach (var item in exportContext.Items)
            {
                var supportedAvatarNames = new List<string>();
                foreach (var supportedAvatarId in AvatarService.GetAllSupportedAvatarIds(item.SupportedAvatars, exportContext.CommonAvatars, exportRequest.IncludeCommonToSupported))
                {
                    var avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, supportedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    supportedAvatarNames.Add(avatarTitle);
                }

                var implementedAvatarNames = new List<string>();
                foreach (var implementedAvatarId in item.ImplementedAvatars.Distinct())
                {
                    var avatarTitle = ItemUtils.GetTitleFromDictionary(avatarTitleMaps, implementedAvatarId);
                    if (string.IsNullOrEmpty(avatarTitle)) continue;

                    implementedAvatarNames.Add(avatarTitle);
                }

                var itemId = CsvUtils.EscapeCsv(item.Id);
                var itemTitle = CsvUtils.EscapeCsv(item.Title);
                var authorName = CsvUtils.EscapeCsv(item.Author);
                var imagePath = CsvUtils.EscapeCsv(item.ThumbnailFileName);

                string categoryName;
                if (item.Type == ItemType.Custom) categoryName = item.CustomCategory;
                else categoryName = exportContext.LocalizedItemTypesMapping.TryGetValue(item.Type, out string? value) ? value : item.Type.ToString();

                var category = CsvUtils.EscapeCsv(categoryName);
                var memo = CsvUtils.EscapeCsv(item.ItemMemo);
                var supportedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, supportedAvatarNames));
                var implementedAvatarsList = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, implementedAvatarNames));
                var boothId = CsvUtils.EscapeCsv(item.BoothId.ToString());
                var itemPath = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.GetFolderPaths()));
                var tags = CsvUtils.EscapeCsv(string.Join(Environment.NewLine, item.Tags));

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
