using System.Text;

namespace AvatarExplorer.Core.Utils;

public static class TextParser
{
    public static string[] Parse(string text)
    {
        List<string> result = new();

        StringBuilder currentArg = new();
        bool inQuotes = false;
        char quoteChar = '\0';

        foreach (char c in text)
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
