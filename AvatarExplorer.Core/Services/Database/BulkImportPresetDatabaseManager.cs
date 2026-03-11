using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Database;

internal class BulkImportPresetDatabaseManager : AbstractDatabaseManager<BulkImportPreset>
{
    public override string DatabaseFilePath { get; } = SystemPath.BulkImportPresetDatabasePath;
}
