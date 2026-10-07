namespace AvatarExplorer.Core.Utils;

/// <summary>
/// かなをローマ字に変換するユーティリティを提供します。
/// </summary>
public static class JapanesePhoneticsHelper
{
    private static readonly Dictionary<string, string[]> RomajiMap = new()
    {
        ["ぁ"] = ["xa"], ["あ"] = ["a"], ["ぃ"] = ["xi"], ["い"] = ["i"],
        ["ぅ"] = ["xu"], ["う"] = ["u"], ["ぇ"] = ["xe"], ["え"] = ["e"],
        ["ぉ"] = ["xo"], ["お"] = ["o"],
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

        ["きゃ"] = ["kya", "kixya"], ["きゅ"] = ["kyu", "kixyu"], ["きょ"] = ["kyo", "kixyo"],
        ["しゃ"] = ["sha", "sya", "shixya", "sixya"], ["しゅ"] = ["shu", "syu", "shixyu", "sixyu"], ["しょ"] = ["sho", "syo", "shixyo", "sixyo"],
        ["ちゃ"] = ["cha", "tya", "cya", "chixya", "tixya", "cixya"], ["ちゅ"] = ["chu", "tyu", "cyu", "chixyu", "tixyu", "cixyu"], ["ちょ"] = ["cho", "tyo", "cyo", "chixyo", "tixyo", "cixyo"],
        ["にゃ"] = ["nya", "nixya"], ["にゅ"] = ["nyu", "nixyu"], ["にょ"] = ["nyo", "nixyo"],
        ["ひゃ"] = ["hya", "hixya"], ["ひゅ"] = ["hyu", "hixyu"], ["ひょ"] = ["hyo", "hixyo"],
        ["みゃ"] = ["mya", "mixya"], ["みゅ"] = ["myu", "mixyu"], ["みょ"] = ["myo", "mixyo"],
        ["りゃ"] = ["rya", "rixya"], ["りゅ"] = ["ryu", "rixyu"], ["りょ"] = ["ryo", "rixyo"],
        ["ぎゃ"] = ["gya", "gixya"], ["ぎゅ"] = ["gyu", "gixyu"], ["ぎょ"] = ["gyo", "gixyo"],
        ["じゃ"] = ["ja", "jya", "zya", "jixya", "zixya"], ["じゅ"] = ["ju", "jyu", "zyu", "jixyu", "zixyu"], ["じょ"] = ["jo", "jyo", "zyo", "jixyo", "zixyo"],
        ["びゃ"] = ["bya", "bixya"], ["びゅ"] = ["byu", "bixyu"], ["びょ"] = ["byo", "bixyo"],
        ["ぴゃ"] = ["pya", "pixya"], ["ぴゅ"] = ["pyu", "pixyu"], ["ぴょ"] = ["pyo", "pixyo"],
        ["いぇ"] = ["ye", "ixe"], ["うぃ"] = ["wi", "uxi"], ["うぇ"] = ["we", "uxe"], ["うぉ"] = ["wo", "uxo"],
        ["じぇ"] = ["je", "jye", "zye", "jixe", "zixe"], ["ちぇ"] = ["che", "tye", "cye", "chixe", "tixe", "cixe"],
        ["てぃ"] = ["ti", "texi"], ["でぃ"] = ["di", "dexi"], ["とぅ"] = ["tu", "tuxu"], ["どぅ"] = ["du", "duxu"],
        ["ふぁ"] = ["fa", "fuxa"], ["ふぃ"] = ["fi", "fuxi"], ["ふぇ"] = ["fe", "fuxe"], ["ふぉ"] = ["fo", "fuxo"],
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

    /// <summary>
    /// 指定された文字列のカタカナをひらがなに変換します。カタカナ以外の文字はそのまま返します。
    /// 変換は Unicode のコードポイントの差を利用して行います。
    /// </summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <returns>変換されたひらがなの文字列。</returns>
    public static string ToHiragana(string value)
    {
        // 3040..309F; Hiragana
        // 30A0..30FF; Katakana
        // https://www.unicode.org/Public/UNIDATA/Blocks.txt
        return string.Concat(value.Select(character =>
        {
            if (character is >= '\u30A0' and <= '\u30FF')
            {
                // 伸ばし棒
                if (character == '\u30FC') return '\u30FC';
                return (char)(character - 0x60);
            }
            return character;
        }));
    }

    private static List<string> Append(IEnumerable<string> values, string suffix) =>
        values.Select(value => value + suffix).ToList();

    private static string DoubleInitialConsonant(string spelling)
    {
        if (spelling.Length == 0 || "aeiou".Contains(spelling[0])) return spelling;
        return spelling[0] + spelling;
    }
}
