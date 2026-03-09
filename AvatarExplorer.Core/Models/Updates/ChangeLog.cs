using System.Text;

namespace AvatarExplorer.Core.Models.Updates;

public class ChangeLog
{
    public List<string> Added { get; set; } = new();
    public List<string> Fixed { get; set; } = new();
    public List<string> Changed { get; set; } = new();

    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        void AppendSection(string title, List<string> items)
        {
            if (stringBuilder.Length > 0) stringBuilder.AppendLine();
            
            stringBuilder.AppendLine($"# {title}");
            foreach (string item in items)
            {
                stringBuilder.AppendLine($"・ {item}");
            }
        }

        AppendSection("Added - 追加点", Added);
        AppendSection("Fixed - 修正点", Fixed);
        AppendSection("Changed - 変更点", Changed);

        return stringBuilder.ToString().TrimEnd();
    }

    public void AddRange(ChangeLog changeLog)
    {
        Added.AddRange(changeLog.Added);
        Fixed.AddRange(changeLog.Fixed);
        Changed.AddRange(changeLog.Changed);
    }
}
