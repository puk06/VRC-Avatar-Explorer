namespace AvatarExplorer.Core.Utils;

/// <summary>
/// かなをローマ字に変換するユーティリティを提供します。
/// </summary>
public static class KanaUtils
{
    private static readonly Dictionary<string, string[]> RomajiMap = new()
    {
        ["ぁ"] = ["a"], ["あ"] = ["a"], ["ぃ"] = ["i"], ["い"] = ["i"],
        ["ぅ"] = ["u"], ["う"] = ["u"], ["ぇ"] = ["e"], ["え"] = ["e"],
        ["ぉ"] = ["o"], ["お"] = ["o"],
        ["か"] = ["ka"], ["き"] = ["ki"], ["く"] = ["ku"], ["け"] = ["ke"], ["こ"] = ["ko"],
        ["が"] = ["ga"], ["ぎ"] = ["gi"], ["ぐ"] = ["gu"], ["げ"] = ["ge"], ["ご"] = ["go"],
        ["さ"] = ["sa"], ["し"] = ["shi", "si"], ["す"] = ["su"], ["せ"] = ["se"], ["そ"] = ["so"],
        ["ざ"] = ["za"], ["じ"] = ["ji", "zi"], ["ず"] = ["zu"], ["ぜ"] = ["ze"], ["ぞ"] = ["zo"],
        ["た"] = ["ta"], ["ち"] = ["chi", "ti"], ["つ"] = ["tsu", "tu"], ["て"] = ["te"], ["と"] = ["to"],
        ["だ"] = ["da"], ["ぢ"] = ["ji", "di"], ["づ"] = ["zu", "du"], ["で"] = ["de"], ["ど"] = ["do"],
        ["な"] = ["na"], ["に"] = ["ni"], ["ぬ"] = ["nu"], ["ね"] = ["ne"], ["の"] = ["no"],
        ["は"] = ["ha"], ["ひ"] = ["hi"], ["ふ"] = ["fu", "hu"], ["へ"] = ["he"], ["ほ"] = ["ho"],
        ["ば"] = ["ba"], ["び"] = ["bi"], ["ぶ"] = ["bu"], ["べ"] = ["be"], ["ぼ"] = ["bo"],
        ["ぱ"] = ["pa"], ["ぴ"] = ["pi"], ["ぷ"] = ["pu"], ["ぺ"] = ["pe"], ["ぽ"] = ["po"],
        ["ま"] = ["ma"], ["み"] = ["mi"], ["む"] = ["mu"], ["め"] = ["me"], ["も"] = ["mo"],
        ["や"] = ["ya"], ["ゆ"] = ["yu"], ["よ"] = ["yo"],
        ["ら"] = ["ra"], ["り"] = ["ri"], ["る"] = ["ru"], ["れ"] = ["re"], ["ろ"] = ["ro"],
        ["わ"] = ["wa"], ["ゐ"] = ["wi"], ["ゑ"] = ["we"], ["を"] = ["wo", "o"],
        ["ん"] = ["n", "nn"], ["ゔ"] = ["vu"],

        ["きゃ"] = ["kya"], ["きゅ"] = ["kyu"], ["きょ"] = ["kyo"],
        ["しゃ"] = ["sha", "sya"], ["しゅ"] = ["shu", "syu"], ["しょ"] = ["sho", "syo"],
        ["ちゃ"] = ["cha", "tya", "cya"], ["ちゅ"] = ["chu", "tyu", "cyu"], ["ちょ"] = ["cho", "tyo", "cyo"],
        ["にゃ"] = ["nya"], ["にゅ"] = ["nyu"], ["にょ"] = ["nyo"],
        ["ひゃ"] = ["hya"], ["ひゅ"] = ["hyu"], ["ひょ"] = ["hyo"],
        ["みゃ"] = ["mya"], ["みゅ"] = ["myu"], ["みょ"] = ["myo"],
        ["りゃ"] = ["rya"], ["りゅ"] = ["ryu"], ["りょ"] = ["ryo"],
        ["ぎゃ"] = ["gya"], ["ぎゅ"] = ["gyu"], ["ぎょ"] = ["gyo"],
        ["じゃ"] = ["ja", "jya", "zya"], ["じゅ"] = ["ju", "jyu", "zyu"], ["じょ"] = ["jo", "jyo", "zyo"],
        ["びゃ"] = ["bya"], ["びゅ"] = ["byu"], ["びょ"] = ["byo"],
        ["ぴゃ"] = ["pya"], ["ぴゅ"] = ["pyu"], ["ぴょ"] = ["pyo"],
        ["いぇ"] = ["ye"], ["うぃ"] = ["wi"], ["うぇ"] = ["we"], ["うぉ"] = ["wo"],
        ["じぇ"] = ["je", "jye", "zye"], ["ちぇ"] = ["che", "tye", "cye"],
        ["てぃ"] = ["ti"], ["でぃ"] = ["di"], ["とぅ"] = ["tu"], ["どぅ"] = ["du"],
        ["ふぁ"] = ["fa"], ["ふぃ"] = ["fi"], ["ふぇ"] = ["fe"], ["ふぉ"] = ["fo"],
    };

    /// <summary>
    /// かなをローマ字へ変換します。表記揺れがある場合は、すべての組み合わせを返します。
    /// かな以外の文字はそのまま結果に含めます。
    /// </summary>
    /// <param name="kana">変換対象の文字列。</param>
    /// <returns>変換結果の重複しない配列。</returns>
    public static string[] ToRomaji(string kana)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);
        var current = new List<string> { string.Empty };
        bool sokuon = false;

        int index = 0;
        while (index < kana.Length)
        {
            var start = index;
            var tokenLength = 1;
            string? token = null;
            if (index + 1 < kana.Length)
            {
                var digraph = ToHiragana(kana[index..(index + 2)]);
                if (RomajiMap.ContainsKey(digraph))
                {
                    token = digraph;
                    tokenLength = 2;
                }
            }

            var source = kana[start..(start + tokenLength)];
            index += tokenLength;

            if (token is null)
            {
                var single = ToHiragana(source);
                if (single == "っ")
                {
                    sokuon = true;
                    continue;
                }

                if (single == "ー")
                {
                    current = Append(current, "-");
                    continue;
                }

                token = single;
            }

            if (!RomajiMap.TryGetValue(token, out var spellings))
            {
                current = Append(current, source);
                sokuon = false;
                continue;
            }

            var next = new List<string>();
            foreach (var prefix in current)
            {
                foreach (var spelling in spellings)
                    next.Add(prefix + (sokuon ? DoubleInitialConsonant(spelling) : spelling));
            }

            current = next;
            sokuon = false;
        }

        if (sokuon) current = Append(current, "っ");
        results.UnionWith(current);
        return results.ToArray();
    }

    private static List<string> Append(IEnumerable<string> values, string suffix) =>
        values.Select(value => value + suffix).ToList();

    private static string DoubleInitialConsonant(string spelling)
    {
        if (spelling.Length == 0 || "aeiou".Contains(spelling[0])) return spelling;
        return spelling[0] + spelling;
    }

    private static string ToHiragana(string value)
    {
        return string.Concat(value.Select(character =>
            character is >= '\u30A1' and <= '\u30F6'
                ? (char)(character - ('\u30A1' - '\u3041'))
                : character));
    }
}
