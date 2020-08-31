using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using System;
using Renci.SshNet.Messages.Connection;

namespace Unknown6656.Common
{
    public static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static bool Match(this string input, string pattern, out Match match, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) => input.Match(new Regex(pattern, options), out match);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Match(this string input, Regex regex, out Match match) => (match = regex.Match(input)).Success;

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static bool Match(this string input, string pattern, [MaybeNullWhen(false), NotNullWhen(true)] out ReadOnlyIndexer<string, string>? groups, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
            input.Match(new Regex(pattern, options), out groups);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Match(this string input, Regex regex, [MaybeNullWhen(false), NotNullWhen(true)] out ReadOnlyIndexer<string, string>? groups) =>
            (groups = input.Match(regex, out Match m) ? new ReadOnlyIndexer<string, string>(k => m.Groups[k].ToString()) : null) is { };

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static bool Match(this string input, Dictionary<string, Action<Match>> patterns) => input.Match(patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Match(this string input, Dictionary<Regex, Action<Match>> patterns) => input.Match(patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static bool Match(this string input, params (string pattern, Action<Match> action)[] patterns) =>
            input.Match(patterns.ToArray(p => (new Regex(p.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), p.action)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Match(this string input, params (Regex pattern, Action<Match> action)[] patterns) =>
            input.Match(false, patterns.ToArray(p => (p.pattern, new Func<Match, bool>(m => { p.action(m); return true; }))));

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        [return: MaybeNull]
        public static T Match<T>(this string input, [MaybeNull] T @default, Dictionary<string, Func<Match, T>> patterns) =>
            Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: MaybeNull]
        public static T Match<T>(this string input, [MaybeNull] T @default, Dictionary<Regex, Func<Match, T>> patterns) =>
            Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        [return: MaybeNull]
        public static T Match<T>(this string input, [MaybeNull] T @default, params (string pattern, Func<Match, T> action)[] patterns) =>
            input.Match(@default, patterns.ToArray(p => (new Regex(p.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), p.action)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: MaybeNull]
        public static T Match<T>(this string input, [MaybeNull] T @default, params (Regex pattern, Func<Match, T> action)[] patterns)
        {
            foreach ((Regex pattern, Func<Match, T> action) in patterns ?? Array.Empty<(Regex, Func<Match, T>)>())
                if (input.Match(pattern, out Match m))
                    return action(m);

            return @default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RegexReplace(this string input, string pattern, string repl, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
            Regex.Replace(input, pattern, repl, options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RegexReplace(this string input, string pattern, MatchEvaluator repl, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
            Regex.Replace(input, pattern, repl, options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOccurences(this string input, string search) => (input.Length - input.Replace(search, "").Length) / search.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] SplitIntoLines(this string input, string newline = "\n") => input.Split(newline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSubScript(this string input) => input.Select(ToSubScript).StringConcat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToSuperScript(this string input) => input.Select(ToSuperScript).StringConcat();

        /* TODO : implement text conversions
         *  input:  abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 +-/.:(),;=*
         *          http://qaz.wtf/u/convert.cgi?text=abcdefghijklmnopqrstuvwxyz+ABCDEFGHIJKLMNOPQRSTUVWXYZ+0123456789+%2B-%2F.%3A%28%29%2C%3B%3D*
         *  
         *  ouptut: ⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵ ⠠⠠⠁⠃⠉⠙⠑⠋⠛⠓⠊⠚⠅⠇⠍⠝⠕⠏⠟⠗⠎⠞⠥⠧⠺⠭⠽⠵ ⠼⠚⠁⠃⠉⠙⠑⠋⠛⠓⠊ +⠤⠌⠲⠒⠦⠴⠂⠆=⠔
         *          ᵃᵇᶜᵈᵉᶠᵍʰⁱʲᵏˡᵐⁿᵒᵖqʳˢᵗᵘᵛʷˣʸᶻ ᴬᴮᶜᴰᴱᶠᴳᴴᴵᴶᴷᴸᴹᴺᴼᴾQᴿˢᵀᵁⱽᵂˣʸᶻ
         *          ₐbcdₑfgₕᵢⱼₖₗₘₙₒₚqᵣₛₜᵤᵥwₓyz ₐBCDₑFGₕᵢⱼₖₗₘₙₒₚQᵣₛₜᵤᵥWₓYZ
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
            char x when x > 0x2f && x < 0x3a => (char)(x + 0x2050),
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
            char x when x > 0x2f && x < 0x3a => (char)(x + 0x2040),
            _ => c,
        };
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
}
