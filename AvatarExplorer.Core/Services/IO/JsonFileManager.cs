using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// 型 <typeparamref name="T"/> のオブジェクトを JSON ファイルとして読み書きするための汎用ヘルパークラスです。
/// </summary>
/// <typeparam name="T">読み書きするオブジェクトの型（参照型）。</typeparam>
public static class JsonFileManager<T> where T : class
{
    /// <summary>
    /// 指定したパスの JSON ファイルを読み込み、型 <typeparamref name="T"/> のオブジェクトとしてデシリアライズします。
    /// </summary>
    /// <param name="path">読み込む JSON ファイルのパス。</param>
    /// <returns>読み込み・デシリアライズに成功した場合はオブジェクト、失敗した場合は null。</returns>
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

    /// <summary>
    /// 指定したオブジェクトを JSON にシリアライズし、指定したパスのファイルに保存します。
    /// </summary>
    /// <param name="data">保存するオブジェクト。</param>
    /// <param name="path">保存先のファイルパス。</param>
    public static void Save(T data, string path)
    {
        var result = FileSystemService.SerializeClass(data, path);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save json file: '{path}'.", tag: result.Errors.ToErrorString());
        }
    }
}
