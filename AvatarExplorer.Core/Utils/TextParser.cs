using System.Text;

namespace AvatarExplorer.Core.Utils;

public static class TextParser
{
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
