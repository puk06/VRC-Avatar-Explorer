using System.Text.Json.Serialization;
using AvatarExplorer.Core.Interfaces.Database;

namespace AvatarExplorer.Core.Models.Items;

public abstract class AbstractDatabaseItem : IDatabaseItem
{
    [JsonInclude] public string Id { get; private set; } = Guid.NewGuid().ToString();
}
