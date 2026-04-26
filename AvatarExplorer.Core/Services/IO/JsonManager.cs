using System.Text.Json;

namespace AvatarExplorer.Core.Services.IO;

public static class JsonManager
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, JsonSerializerOptions);
    }

    public static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
