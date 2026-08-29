using System.Text.Json;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// オブジェクトと JSON 文字列の間でシリアライズ・デシリアライズを行う静的クラスです。
/// </summary>
public static class JsonManager
{
    /// <summary>シリアライズ／デシリアライズで共通して使用される <see cref="JsonSerializerOptions"/>（インデント付き・プロパティ名の大文字小文字を無視）を取得します。</summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 指定したオブジェクトを JSON 文字列にシリアライズします。
    /// </summary>
    /// <typeparam name="T">シリアライズするオブジェクトの型。</typeparam>
    /// <param name="obj">シリアライズするオブジェクト。</param>
    /// <returns>シリアライズされた JSON 文字列。</returns>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, JsonSerializerOptions);
    }

    /// <summary>
    /// 指定した JSON 文字列を型 <typeparamref name="T"/> のオブジェクトにデシリアライズします。
    /// </summary>
    /// <typeparam name="T">デシリアライズ先の型（参照型）。</typeparam>
    /// <param name="json">デシリアライズ対象の JSON 文字列。</param>
    /// <returns>成功した場合はオブジェクト、失敗した場合は null。</returns>
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
