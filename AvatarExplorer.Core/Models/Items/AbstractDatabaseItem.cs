using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.Database;

namespace AvatarExplorer.Core.Models.Items;

public abstract class AbstractDatabaseItem : IDatabaseItem
{
#pragma warning disable RCS1170
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
#pragma warning restore RCS1170
}
