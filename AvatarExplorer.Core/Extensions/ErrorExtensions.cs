using ErrorOr;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// ErrorOr の <see cref="Error"/> コレクションに対する拡張メソッドを提供します。
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// エラーのリストを、各エラーの説明をカンマで連結した文字列に変換します。
    /// </summary>
    /// <param name="errors">エラーのリスト。</param>
    /// <returns>説明をカンマ区切りで連結した文字列。</returns>
    public static string ToErrorString(this List<Error> errors)
    {
        return string.Join(", ", errors.Select(e => e.Description));
    }
}
