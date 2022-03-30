using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System;

using Unknown6656.Generics;

namespace Unknown6656.Common;


public static class StringExtensions
{
    [Obsolete]
    public static bool Match(this string input, string pattern, out Match match, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) => input.Match(new Regex(pattern, options), out match);

    [Obsolete]
    public static bool Match(this string input, string pattern, [MaybeNullWhen(false), NotNullWhen(true)] out ReadOnlyIndexer<string, string>? groups, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
        input.Match(new Regex(pattern, options), out groups);

    [Obsolete]
    public static bool Match(this string input, Dictionary<string, Action<Match>> patterns) => input.Match(patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

    [Obsolete]
    public static bool Match(this string input, params (string pattern, Action<Match> action)[] patterns) =>
        input.Match(patterns.ToArray(p => (new Regex(p.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), p.action)));

    [Obsolete]
    [return: MaybeNull]
    public static T Match<T>(this string input, [MaybeNull] T @default, Dictionary<string, Func<Match, T>> patterns) =>
        Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

    [Obsolete]
    [return: MaybeNull]
    public static T Match<T>(this string input, [MaybeNull] T @default, params (string pattern, Func<Match, T> action)[] patterns) =>
        input.Match(@default, patterns.ToArray(p => (new Regex(p.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), p.action)));

    public static bool Match(this string input, Regex regex, out Match match) => (match = regex.Match(input)).Success;

    public static bool Match(this string input, Regex regex, [MaybeNullWhen(false), NotNullWhen(true)] out ReadOnlyIndexer<string, string>? groups) =>
        (groups = input.Match(regex, out Match m) ? new ReadOnlyIndexer<string, string>(k => m.Groups[k].ToString()) : null) is { };

    public static bool Match(this string input, Dictionary<Regex, Action<Match>> patterns) => input.Match(patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

    public static bool Match(this string input, params (Regex pattern, Action<Match> action)[] patterns) =>
        input.Match(false, patterns.ToArray(p => (p.pattern, new Func<Match, bool>(m => { p.action(m); return true; }))));

    [return: MaybeNull]
    public static T Match<T>(this string input, [MaybeNull] T @default, Dictionary<Regex, Func<Match, T>> patterns) =>
        Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

    [return: MaybeNull]
    public static T Match<T>(this string input, [MaybeNull] T @default, params (Regex pattern, Func<Match, T> action)[] patterns)
    {
        foreach ((Regex pattern, Func<Match, T> action) in patterns ?? Array.Empty<(Regex, Func<Match, T>)>())
            if (input.Match(pattern, out Match m))
                return action(m);

        return @default;
    }

    public static string RegexReplace(this string input, string pattern, string repl, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
        Regex.Replace(input, pattern, repl, options);

    public static string RegexReplace(this string input, string pattern, MatchEvaluator repl, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
        Regex.Replace(input, pattern, repl, options);

    public static int CountOccurences(this string input, string search) => (input.Length - input.Replace(search, "").Length) / search.Length;

    public static string[] SplitIntoLines(this string input, string newline = "\n") => input.Split(newline);

    public static string Replace(this string input, params (char from, char to)[] replacements) => input.Replace(replacements as IEnumerable<(char, char)>);

    public static string Replace(this string input, params (string from, string to)[] replacements) => input.Replace(replacements as IEnumerable<(string, string)>);

    public static string Replace(this string input, IEnumerable<(char from, char to)> replacements) =>
        replacements.Aggregate(input, (str, repl) => str.Replace(repl.from, repl.to));

    public static string Replace(this string input, IEnumerable<(string from, string to)> replacements) =>
        replacements.Aggregate(input, (str, repl) => str.Replace(repl.from, repl.to));

    public static string Remove(this string input, string search) => input.Replace(search, string.Empty);

    public static string GetCommonSuffix(params string[] words)
    {
        string suffix = words[0];
        int len = suffix.Length;

        for (int i = 1, l = words.Length; i < l; i++)
        {
            string word = words[i];

            if (!word.EndsWith(suffix))
            {
                int wordlen = word.Length;
                int max = wordlen < len ? wordlen : len;

                if (max == 0)
                    return "";

                for (int j = 1; j < max; j++)
                    if (suffix[len - j] != word[wordlen - j])
                    {
                        suffix = suffix.Substring(len - j + 1, j - 1);
                        len = j - 1;

                        break;
                    }
            }
        }

        return suffix;
    }

    public static string GetCommonPrefix(params string[] words)
    {
        string suffix = words[0];
        int len = suffix.Length;

        for (int i = 1, l = words.Length; i < l; i++)
        {
            string word = words[i];

            if (!word.StartsWith(suffix))
            {
                int wordlen = word.Length;
                int max = wordlen < len ? wordlen : len;

                if (max == 0)
                    return "";

                for (int j = 1; j < max; j++)
                    if (suffix[j] != word[j])
                    {
                        suffix = word.Substring(0, j);
                        len = j + 1;

                        break;
                    }
            }
        }

        return suffix;
    }

    public static string ToSubScript(this string input) => input.Select(ToSubScript).StringConcat();

    public static string ToSuperScript(this string input) => input.Select(ToSuperScript).StringConcat();

    /* TODO : implement text conversions
     *  input:  abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 +-/.:(),;=*
     *          http://qaz.wtf/u/convert.cgi?text=abcdefghijklmnopqrstuvwxyz+ABCDEFGHIJKLMNOPQRSTUVWXYZ+0123456789+%2B-%2F.%3A%28%29%2C%3B%3D*
     *  
     *  ouptut: ⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵ ⠠⠠⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵ ⠼⠚⠁⠃⠉⠙⠑⠋⠛⠓⠊ +⠤⠌⠲⠒⠦⠴⠂⠆=⠔
     *          𝐚𝐛𝐜𝐝𝐞𝐟𝐠𝐡𝐢𝐣𝐤𝐥𝐦𝐧𝐨𝐩𝐪𝐫𝐬𝐭𝐮𝐯𝐰𝐱𝐲𝐳 𝐀𝐁𝐂𝐃𝐄𝐅𝐆𝐇𝐈𝐉𝐊𝐋𝐌𝐍𝐎𝐏𝐐𝐑𝐒𝐓𝐔𝐕𝐖𝐗𝐘𝐙 𝟎𝟏𝟐𝟑𝟒𝟓𝟔𝟕𝟖𝟗
     *          𝖆𝖇𝖈𝖉𝖊𝖋𝖌𝖍𝖎𝖏𝖐𝖑𝖒𝖓𝖔𝖕𝖖𝖗𝖘𝖙𝖚𝖛𝖜𝖝𝖞𝖟 𝕬𝕭𝕮𝕯𝕰𝕱𝕲𝕳𝕴𝕵𝕶𝕷𝕸𝕹𝕺𝕻𝕼𝕽𝕾𝕿𝖀𝖁𝖂𝖃𝖄𝖅
     *          𝒂𝒃𝒄𝒅𝒆𝒇𝒈𝒉𝒊𝒋𝒌𝒍𝒎𝒏𝒐𝒑𝒒𝒓𝒔𝒕𝒖𝒗𝒘𝒙𝒚𝒛 𝑨𝑩𝑪𝑫𝑬𝑭𝑮𝑯𝑰𝑱𝑲𝑳𝑴𝑵𝑶𝑷𝑸𝑹𝑺𝑻𝑼𝑽𝑾𝑿𝒀𝒁
     *          𝓪𝓫𝓬𝓭𝓮𝓯𝓰𝓱𝓲𝓳𝓴𝓵𝓶𝓷𝓸𝓹𝓺𝓻𝓼𝓽𝓾𝓿𝔀𝔁𝔂𝔃 𝓐𝓑𝓒𝓓𝓔𝓕𝓖𝓗𝓘𝓙𝓚𝓛𝓜𝓝𝓞𝓟𝓠𝓡𝓢𝓣𝓤𝓥𝓦𝓧𝓨𝓩
     *          𝕒𝕓𝕔𝕕𝕖𝕗𝕘𝕙𝕚𝕛𝕜𝕝𝕞𝕟𝕠𝕡𝕢𝕣𝕤𝕥𝕦𝕧𝕨𝕩𝕪𝕫 𝔸𝔹ℂ𝔻𝔼𝔽𝔾ℍ𝕀𝕁𝕂𝕃𝕄ℕ𝕆ℙℚℝ𝕊𝕋𝕌𝕍𝕎𝕏𝕐ℤ 𝟘𝟙𝟚𝟛𝟜𝟝𝟞𝟟𝟠𝟡
     *          𝚊𝚋𝚌𝚍𝚎𝚏𝚐𝚑𝚒𝚓𝚔𝚕𝚖𝚗𝚘𝚙𝚚𝚛𝚜𝚝𝚞𝚟𝚠𝚡𝚢𝚣 𝙰𝙱𝙲𝙳𝙴𝙵𝙶𝙷𝙸𝙹𝙺𝙻𝙼𝙽𝙾𝙿𝚀𝚁𝚂𝚃𝚄𝚅𝚆𝚇𝚈𝚉
     *          𝖺𝖻𝖼𝖽𝖾𝖿𝗀𝗁𝗂𝗃𝗄𝗅𝗆𝗇𝗈𝗉𝗊𝗋𝗌𝗍𝗎𝗏𝗐𝗑𝗒𝗓 𝖠𝖡𝖢𝖣𝖤𝖥𝖦𝖧𝖨𝖩𝖪𝖫𝖬𝖭𝖮𝖯𝖰𝖱𝖲𝖳𝖴𝖵𝖶𝖷𝖸𝖹
     *          𝗮𝗯𝗰𝗱𝗲𝗳𝗴𝗵𝗶𝗷𝗸𝗹𝗺𝗻𝗼𝗽𝗾𝗿𝘀𝘁𝘂𝘃𝘄𝘅𝘆𝘇 𝗔𝗕𝗖𝗗𝗘𝗙𝗚𝗛𝗜𝗝𝗞𝗟𝗠𝗡𝗢𝗣𝗤𝗥𝗦𝗧𝗨𝗩𝗪𝗫𝗬𝗭 𝟬𝟭𝟮𝟯𝟰𝟱𝟲𝟳𝟴𝟵
     *          𝙖𝙗𝙘𝙙𝙚𝙛𝙜𝙝𝙞𝙟𝙠𝙡𝙢𝙣𝙤𝙥𝙦𝙧𝙨𝙩𝙪𝙫𝙬𝙭𝙮𝙯 𝘼𝘽𝘾𝘿𝙀𝙁𝙂𝙃𝙄𝙅𝙆𝙇𝙈𝙉𝙊𝙋𝙌𝙍𝙎𝙏𝙐𝙑𝙒𝙓𝙔𝙕
     *          𝘢𝘣𝘤𝘥𝘦𝘧𝘨𝘩𝘪𝘫𝘬𝘭𝘮𝘯𝘰𝘱𝘲𝘳𝘴𝘵𝘶𝘷𝘸𝘹𝘺𝘻 𝘈𝘉𝘊𝘋𝘌𝘍𝘎𝘏𝘐𝘑𝘒𝘓𝘔𝘕𝘖𝘗𝘘𝘙𝘚𝘛𝘜𝘝𝘞𝘟𝘠𝘡
     *          ᴀʙᴄᴅᴇꜰɢʜɪᴊᴋʟᴍɴᴏᴩqʀꜱᴛᴜᴠᴡxyᴢ ᴀʙᴄᴅᴇꜰɢʜɪᴊᴋʟᴍɴᴏᴩQʀꜱᴛᴜᴠᴡxYᴢ
     */

    public static char ToSubScript(this char c) => c switch
    {
        ',' => '⸳',
        '.' => '⸳',
        '+' => '₊',
        '-' => '₋',
        '=' => '₌',
        '(' => '₍',
        ')' => '₎',
        'a' or 'A' => 'ₐ',
        'e' or 'E' => 'ₑ',
        'h' or 'H' => 'ₕ',
        'i' or 'I' => 'ᵢ',
        'j' or 'J' => 'ⱼ',
        'k' or 'K' => 'ₖ',
        'l' or 'L' => 'ₗ',
        'm' or 'M' => 'ₘ',
        'n' or 'N' => 'ₙ',
        'o' or 'O' => 'ₒ',
        'p' or 'P' => 'ₚ',
        'r' or 'R' => 'ᵣ',
        's' or 'S' => 'ₛ',
        't' or 'T' => 'ₜ',
        'u' or 'U' => 'ᵤ',
        'v' or 'V' => 'ᵥ',
        'x' or 'X' => 'ₓ',
        > '\x2f' and < '\x3a' => (char)(c + 0x2050),
        _ => c,
    };

    public static char ToSuperScript(this char c) => c switch
    {
        ',' => '⋅',
        '.' => '⋅',
        '+' => '⁺',
        '-' => '⁻',
        '=' => '⁼',
        '(' => '⁽',
        ')' => '⁾',
        '1' => '¹',
        '2' => '²',
        '3' => '³',
        'a' => 'ᵃ',
        'b' => 'ᵇ',
        'c' or 'C' => 'ᶜ',
        'd' => 'ᵈ',
        'e' => 'ᵉ',
        'f' or 'F' => 'ᶠ',
        'g' => 'ᵍ',
        'h' => 'ʰ',
        'i' => 'ⁱ',
        'j' => 'ʲ',
        'k' => 'ᵏ',
        'l' => 'ˡ',
        'm' => 'ᵐ',
        'n' => 'ⁿ',
        'o' => 'ᵒ',
        'p' => 'ᵖ',
        'r' => 'ʳ',
        's' or 'S' => 'ˢ',
        't' => 'ᵗ',
        'u' => 'ᵘ',
        'v' => 'ᵛ',
        'w' => 'ʷ',
        'x' or 'X' => 'ˣ',
        'y' or 'Y' => 'ʸ',
        'z' or 'Z' => 'ᶻ',
        'A' => 'ᴬ',
        'B' => 'ᴮ',
        'D' => 'ᴰ',
        'E' => 'ᴱ',
        'G' => 'ᴳ',
        'H' => 'ᴴ',
        'I' => 'ᴵ',
        'J' => 'ᴶ',
        'K' => 'ᴷ',
        'L' => 'ᴸ',
        'M' => 'ᴹ',
        'N' => 'ᴺ',
        'O' => 'ᴼ',
        'P' => 'ᴾ',
        'R' => 'ᴿ',
        'T' => 'ᵀ',
        'U' => 'ᵁ',
        'V' => 'ⱽ',
        'W' => 'ᵂ',
        > '\x2f' and < '\x3a' => (char)(c + 0x2040),
        _ => c,
    };

    public static string? ToPunycode(this string str) => ToPunycode(str, PunycodeConfig.Default);

    public static string? ToPunycode(this string str, PunycodeConfig config)
    {
        (List<char> output, List<char> non_basic) = str.Partition(config.NeedsToBeEscaped);
        int h = output.Count;
        int b = h;

        if (h > 0)
            output.Add('-');

        int n = config.INITIAL_N;
        int bias = config.INITIAL_BIAS;
        int delta = 0;

        while (h < str.Length)
            if (punycode_next_smallest_codepoint(non_basic, n) is int m)
            {
                delta += (m - n) * (h + 1);
                n = m;

                foreach (char c in str)
                    if (c < n)
                        ++delta;
                    else if (c == n)
                    {
                        output.AddRange(punycode_encode(bias, delta, config).Select(c => (char)c));
                        bias = punycode_adapt_bias(delta, h + 1, b == h, config);
                        delta = 0;
                        ++h;
                    }

                ++delta;
                ++n;
            }
            else
                return null;

        return new(output.ToArray());
    }

    public static string? FromPunycode(this string str) => FromPunycode(str, PunycodeConfig.Default);

    public static string? FromPunycode(this string str, PunycodeConfig config)
    {
        int b = Math.Max(0, 1 + str.LastIndexOf('-'));
        List<char> output = b > 0 ? str.Take(b - 1).ToList() : new();
        int bias = config.INITIAL_BIAS;
        int n = config.INITIAL_N;
        int i = 0;

        while (b < str.Length)
        {
            int org_i = i;
            int k = config.BASE;
            int w = 1;

            while (true)
                if (punycode_decode(str[b]) is int d)
                {
                    int t = punycode_threshold(k, bias, config);

                    ++b;
                    i += d * w;

                    if (d < t)
                        break;

                    w *= config.BASE - t;
                    k += config.BASE;
                }
                else
                    return null;

            int x = output.Count + 1;

            bias = punycode_adapt_bias(i - org_i, x, org_i == 0, config);
            n += i / x;
            i %= x;
            output.Insert(i, (char)n);
            ++i;
        }

        return new(output.ToArray());
    }

    private static int punycode_encode(int d) => d + (d < 26 ? 97 : 22);

    private static int? punycode_decode(int d) => d switch
    {
        >= '0' and <= '9' => d - 22,
        >= 'A' and <= 'Z' => d - 65,
        >= 'a' and <= 'z' => d - 97,
        _ => null
    };

    private static int? punycode_next_smallest_codepoint(IEnumerable<char> non_basic, int n)
    {
        const int max = 0x110000; // Unicode's upper bound + 1
        int m = max;

        foreach (char c in non_basic)
            if (c >= n && c < m)
                m = c;

        return m < max ? m : null;
    }

    private static int punycode_adapt_bias(int delta, int n_points, bool is_first, PunycodeConfig config)
    {
        delta /= is_first ? config.DAMP : 2;
        delta += delta / n_points;

        int s = config.BASE - config.T_MIN;
        int t = s * config.T_MAX / 2; // threshold=455
        int k = 0;

        while (delta > t)
        {
            delta /= s;
            k += config.BASE;
        }

        int a = (config.BASE - config.T_MIN + 1) * delta;
        int b = delta + config.SKEW;

        return k + (a / b);
    }

    private static int punycode_threshold(int k, int bias, PunycodeConfig config) => k <= bias + config.T_MIN ? config.T_MIN
                                                                                  : k >= bias + config.T_MAX ? config.T_MAX
                                                                                  : k - bias;

    private static List<int> punycode_encode(int bias, int delta, PunycodeConfig config)
    {
        List<int> result = new();
        int k = config.BASE;
        int q = delta;

        while (punycode_threshold(k, bias, config) is int t)
            if (q < t)
            {
                result.Add(punycode_encode(q));
                break;
            }
            else
            {
                int c = t + ((q - t) % (config.BASE - t));

                q = (q - t) / (config.BASE - t);
                k += config.BASE;
                result.Add(punycode_encode(c));
            }

        return result;
    }
}

public record class PunycodeConfig(
    Predicate<char> NeedsToBeEscaped,
    int T_MIN,
    int T_MAX,
    int BASE,
    int SKEW,
    int DAMP,
    int INITIAL_N,
    int INITIAL_BIAS
)
{
    public static PunycodeConfig Default { get; } = new(c => c is < '\x20' or > '\x7e', 1, 26, 36, 38, 700, 128, 72);

    internal void Validate()
    {
        if (T_MIN < 0)
            throw new ArgumentException("The value must not be negative.", nameof(T_MIN));
        else if (T_MIN > T_MAX)
            throw new ArgumentException("T_MAX must be greater or equal to T_MIN.", nameof(T_MAX));
        else if (T_MAX >= BASE)
            throw new ArgumentException("T_MAX must be smaller than BASE.", nameof(T_MAX));
        else if (SKEW < 1)
            throw new ArgumentException("The value must be greater than zero.", nameof(SKEW));
        else if (DAMP < 2)
            throw new ArgumentException("The value must be greater than 1.", nameof(DAMP));
        else if ((INITIAL_BIAS % BASE) > (BASE - T_MIN))
            throw new ArgumentException("INITIAL_BIAS % BASE  must be smaller or equal to  BASE - T_MIN.", nameof(INITIAL_BIAS));
    }
}

public sealed class BytewiseEncodingProvider
    : EncodingProvider
{
    public static BytewiseEncodingProvider Instance { get; }


    static BytewiseEncodingProvider() => Encoding.RegisterProvider(Instance = new BytewiseEncodingProvider());

    private BytewiseEncodingProvider()
    {
    }

    public override Encoding? GetEncoding(int codepage) => codepage == BytewiseEncoding.Codepage ? BytewiseEncoding.Instance : null;

    public override Encoding? GetEncoding(string? name) => name?.Equals("", StringComparison.InvariantCultureIgnoreCase) ?? false ? BytewiseEncoding.Instance : null;
}

public sealed class BytewiseEncoding
    : Encoding
{
    public const int Codepage = 0x420;
    public static BytewiseEncoding Instance { get; } = new BytewiseEncoding();


    private BytewiseEncoding()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetByteCount(char[] chars, int index, int count) => count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        for (int i = 0; i < charCount; ++i)
            bytes[byteIndex + i] = (byte)chars[charIndex + i];

        return charCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetCharCount(byte[] bytes, int index, int count) => count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        for (int i = 0; i < byteCount; ++i)
            chars[charIndex + i] = (char)bytes[byteIndex + i];

        return byteCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetMaxByteCount(int charCount) => charCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetMaxCharCount(int byteCount) => byteCount;
}
