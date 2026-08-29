using System.Text;

namespace AvatarExplorer.Core.Models.Updates;

/// <summary>
/// バージョンの変更履歴（追加・修正・変更点）を表します。
/// </summary>
public class ChangeLog
{
    /// <summary>
    /// 追加された項目の一覧。
    /// </summary>
    public List<string> Added { get; set; } = [];

    /// <summary>
    /// 修正された項目の一覧。
    /// </summary>
    public List<string> Fixed { get; set; } = [];

    /// <summary>
    /// 変更された項目の一覧。
    /// </summary>
    public List<string> Changed { get; set; } = [];

    /// <summary>
    /// この変更履歴を Markdown 風の文字列として返します。
    /// </summary>
    /// <returns>セクションごとに整形された変更履歴の文字列。</returns>
    public override string ToString()
    {
        var  stringBuilder = new StringBuilder();

        void AppendSection(string title, List<string> items)
        {
            if (stringBuilder.Length > 0) stringBuilder.AppendLine();
            stringBuilder.AppendLine($"# {title}");
            foreach (var item in items) stringBuilder.AppendLine($"・ {item}");
        }

        AppendSection("Added - 追加点", Added);
        AppendSection("Fixed - 修正点", Fixed);
        AppendSection("Changed - 変更点", Changed);

        return stringBuilder.ToString().TrimEnd();
    }

    /// <summary>
    /// 別の変更履歴の内容をこのインスタンスに追加（マージ）します。
    /// </summary>
    /// <param name="changeLog">マージ元の変更履歴。</param>
    public void AddRange(ChangeLog changeLog)
    {
        Added.AddRange(changeLog.Added);
        Fixed.AddRange(changeLog.Fixed);
        Changed.AddRange(changeLog.Changed);
    }
}
