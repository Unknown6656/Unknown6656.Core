using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;

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
        public static T Match<T>(this string input, T @default, Dictionary<string, Func<Match, T>> patterns) =>
            Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Match<T>(this string input, T @default, Dictionary<Regex, Func<Match, T>> patterns) =>
            Match(input, @default, patterns.ToArray(kvp => (kvp.Key, kvp.Value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining), Obsolete]
        public static T Match<T>(this string input, T @default, params (string pattern, Func<Match, T> action)[] patterns) =>
            input.Match(@default, patterns.ToArray(p => (new Regex(p.pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), p.action)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Match<T>(this string input, T @default, params (Regex pattern, Func<Match, T> action)[] patterns)
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
