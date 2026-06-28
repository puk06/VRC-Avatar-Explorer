using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

public static class JsonFileManager<T> where T : class
{
    public static T? Load(string path)
    {
        var result = FileSystemService.DeserializeClass<T>(path);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError($"Failed to load json file: '{path}'.", tag: result.Errors.ToErrorString());
            return null;
        }

        return result.Value;
    }

    public static void Save(T data, string path)
    {
        var result = FileSystemService.SerializeClass(data, path);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save json file: '{path}'.", tag: result.Errors.ToErrorString());
        }
    }
}
