using System.Text;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// 引用符と空白を考慮して文字列をトークンに分割（構文解析）するユーティリティを提供します。
/// </summary>
public static class TextParser
{
    /// <summary>
    /// 文字列を引数のリストとして解析します。空白で区切り、ダブルクォート（"）またはシングルクォート（'）で囲まれた部分はその内部の空白を含めて1つのトークンとします。
    /// </summary>
    /// <param name="text">解析対象の文字列。</param>
    /// <returns>分割されたトークンの配列。</returns>
    public static string[] Parse(string text)
    {
        var result = new List<string>();

        var currentArg = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';

        foreach (var c in text)
        {
            if (c == '"' || c == '\'')
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                    continue;
                }

                if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                    continue;
                }
            }

            if (c == ' ' && !inQuotes)
            {
                if (currentArg.Length > 0)
                {
                    result.Add(currentArg.ToString());
                    currentArg.Clear();
                }

                continue;
            }

            currentArg.Append(c);
        }

        if (currentArg.Length > 0) result.Add(currentArg.ToString());

        return result.ToArray();
    }
}
