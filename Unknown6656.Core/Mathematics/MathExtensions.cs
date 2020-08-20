#nullable enable

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Common;

using static System.Math;

using bint = System.Numerics.BigInteger;


namespace Unknown6656.Mathematics
{
    public static partial class MathExtensions
    {
        public const decimal M_PI = 3.14159265358979323846m;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] GetOffsets(this Range range, int length, int offset = 0)
        {
            int start = range.Start.GetOffset(length) + offset;
            int end = range.End.GetOffset(length) + offset;
            int[] arr = new int[end - start];

            for (int i = 0; i < arr.Length; ++i)
                arr[i] = start + i;

            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(this int l) => ((l - 1) & l) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(this uint l) => ((l - 1) & l) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(this long l) => ((l - 1) & l) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOf2(this ulong l) => ((l - 1) & l) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this double v) => Abs(v) <= 2 * double.Epsilon;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(this double x) => x.Clamp(0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(this double x, double low, double high) => x < low ? low : x > high ? high : x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(this double x, double y) => Scalar.Is(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Are(this IEnumerable<double> xs, IEnumerable<double> ys) => xs.Are(ys, Scalar.EqualityComparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Are(this IEnumerable<Scalar> xs, IEnumerable<Scalar> ys) => xs.Are(ys, Scalar.EqualityComparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Are(this IEnumerable<float> xs, IEnumerable<float> ys) => xs.Are(ys, Scalar.EqualityComparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FastPow(this double @base, double exp) => Exp(Log(@base) * exp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ACosh(this double x) => x < 1 ? 0 : Log(x + Sqrt(x * x - 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ASinh(this double x) => (x < 0 ? -1 : x > 0 ? 1 : 0) * Log(Abs(x) + Sqrt(1 + x * x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ATanh(this double x) => Abs(x) >= 1 ? 0 : .5f * Log((1 + x) / (1 - x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Degrees(this double radians) => radians * 57.295779513082320876798154814105;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Degrees(this Scalar radians) => radians * 57.295779513082320876798154814105;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Radians(this double degrees) => degrees * 0.01745329251994329576923690768489;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Radians(this Scalar degrees) => degrees * 0.01745329251994329576923690768489;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ROL(this long l, int i) => i < 0 ? ROR(l, -i) : i == 0 ? l : (i << (i % 64)) | (i >> (64 - i % 64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ROR(this long l, int i) => i < 0 ? ROL(l, -i) : i == 0 ? l : (i >> (i % 64)) | (i << (64 - i % 64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ROL(this ulong l, int i) => i < 0 ? ROR(l, -i) : i == 0 ? l : (ulong)(i << (i % 64)) | (ulong)(i >> (64 - i % 64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ROR(this ulong l, int i) => i < 0 ? ROL(l, -i) : i == 0 ? l : (ulong)(i >> (i % 64)) | (ulong)(i << (64 - i % 64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ROL(this int l, int i) => i < 0 ? ROR(l, -i) : i == 0 ? l : (i << (i % 32)) | (i >> (32 - i % 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ROR(this int l, int i) => i < 0 ? ROL(l, -i) : i == 0 ? l : (i >> (i % 32)) | (i << (32 - i % 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ROL(this uint l, int i) => i < 0 ? ROR(l, -i) : i == 0 ? l : (uint)(i << (i % 32)) | (uint)(i >> (32 - i % 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ROR(this uint l, int i) => i < 0 ? ROL(l, -i) : i == 0 ? l : (uint)(i >> (i % 32)) | (uint)(i << (32 - i % 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] XOR(this byte[] arr1, byte[] arr2)
        {
            byte[] res = new byte[Min(arr1.Length, arr2.Length)];

            Parallel.For(0, res.Length, i => res[i] = (byte)(arr1[i] ^ arr2[i]));

            return res;
        }

        public static ulong BinomialCoefficient(ulong n, ulong k)
        {
            if (k > n)
                return 0;

            ulong r = 1;

            for (ulong d = 1; d <= k; ++d)
            {
                r *= n--;
                r /= d;
            }

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GreatestCommonDivisor(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
                if (a > b)
                    a %= b;
                else
                    b %= a;

            return a == 0 ? b : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bint GreatestCommonDivisor(bint a, bint b)
        {
            if (a < 0)
                a = -a;

            if (b < 0)
                b = -b;

            while (!a.IsZero && !b.IsZero)
                if (a > b)
                    a %= b;
                else
                    b %= a;

            return a.IsZero ? b : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bint LeastCommonMultiple(bint a, bint b) => (a / GreatestCommonDivisor(a, b)) * b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LeastCommonMultiple(ulong a, ulong b) => (a / GreatestCommonDivisor(a, b)) * b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrime(this bint number)
        {
            if (number < 2)
                return false;

            int limit = _primes[^1];

            if (number <= limit)
                return Array.BinarySearch(_primes, number) >= 0;

            return !Enumerable.Range(limit + 1, (int)Sqrt((double)number) - limit).AsParallel().Select(i => number % i).Any(i => i == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bint[] PrimeFactorization(this bint a)
        {
            List<bint> f = new List<bint>();

            for (bint b = 2; a > 1; ++b)
                while (a % b == 0)
                {
                    a /= b;
                    f.Add(b);
                }

            return f.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bint? Phi(this bint a) => a.PrimeFactorization().ToArray() is { Length: 2 } l ? (bint?)((l[0] - 1) * (l[1] - 1)) : null;

        public static string ToSuperScript(this Scalar l) => l.ToString().ToSuperScript();

        public static string ToSubScript(this Scalar l) => l.ToString().ToSubScript();

        public static string ToSuperScript(this decimal l) => l.ToString().ToSuperScript();

        public static string ToSubScript(this decimal l) => l.ToString().ToSubScript();

        public static string ToSuperScript(this long l) => l.ToString().ToSuperScript();

        public static string ToSubScript(this long l) => l.ToString().ToSubScript();

        public static string ToSuperScript(this ulong l) => l.ToString().ToSuperScript();

        public static string ToSubScript(this ulong l) => l.ToString().ToSubScript();

        public static string ToHumanReadableSize(this long l)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "EB", "PB" };
            int order = 0;

            while (l >= 1024 && order < sizes.Length - 1)
            {
                ++order;

                l /= 1024;
            }

            return $"{l:0.##} {sizes[order]}";
        }

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
    }
}
