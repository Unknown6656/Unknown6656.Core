#nullable enable

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
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
        public static float Constrain(this float scalar, float min, float max) => scalar <= min ? min : scalar >= max ? max : scalar;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Constrain(this double scalar, double min, double max) => scalar <= min ? min : scalar >= max ? max : scalar;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Constrain(this decimal scalar, decimal min, decimal max) => scalar <= min ? min : scalar >= max ? max : scalar;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Map(this float scalar, (float lower, float upper) from, (float lower, float upper) to) =>
            (scalar - from.lower) / (from.upper - from.lower) * (to.upper - to.lower) + to.lower;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Map(this double scalar, (double lower, double upper) from, (double lower, double upper) to) =>
            (scalar - from.lower) / (from.upper - from.lower) * (to.upper - to.lower) + to.lower;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Map(this decimal scalar, (decimal lower, decimal upper) from, (decimal lower, decimal upper) to) =>
            (scalar - from.lower) / (from.upper - from.lower) * (to.upper - to.lower) + to.lower;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ConstrainMap(this float scalar, (float lower, float upper) from, (float lower, float upper) to) => scalar.Constrain(from.lower, from.upper).Map(from, to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConstrainMap(this double scalar, (double lower, double upper) from, (double lower, double upper) to) => scalar.Constrain(from.lower, from.upper).Map(from, to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ConstrainMap(this decimal scalar, (decimal lower, decimal upper) from, (decimal lower, decimal upper) to) => scalar.Constrain(from.lower, from.upper).Map(from, to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Product(this IEnumerable<float> scalars) => scalars.Aggregate(1f, (s1, s2) => s1 * s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Product(this IEnumerable<double> scalars) => scalars.Aggregate(1d, (s1, s2) => s1 * s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Product(this IEnumerable<decimal> scalars) => scalars.Aggregate(1m, (s1, s2) => s1 * s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: MaybeNull]
        public static T Median<T>(this IEnumerable<T> scalars)
            where T : IComparable<T>
        {
            T[] ordered = scalars.OrderBy(LINQ.id).ToArray();

            return ordered.Length == 0 ? default : ordered[ordered.Length / 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average(this IEnumerable<float>? scalars) => (scalars as float[] ?? scalars?.ToArray()) is float[] arr ? arr.Sum() / arr.Length : 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Average(this IEnumerable<double>? scalars) => (scalars as double[] ?? scalars?.ToArray()) is double[] arr ? arr.Sum() / arr.Length : 0d;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Average(this IEnumerable<decimal>? scalars) => (scalars as decimal[] ?? scalars?.ToArray()) is decimal[] arr ? arr.Sum() / arr.Length : 0m;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Variance(this IEnumerable<float>? scalars) => (float)Variance(scalars?.Select(Convert.ToDouble));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Variance(this IEnumerable<double>? scalars)
        {
            double avg = scalars.Average();

            return Math.Pow(scalars?.Sum(x => x - avg) ?? 0, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float StandardDeviation(this IEnumerable<float>? scalars) => (float)StandardDeviation(scalars?.Select(Convert.ToDouble));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double StandardDeviation(this IEnumerable<double>? scalars) => Math.Sqrt(scalars.Variance());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal StandardDeviation(this IEnumerable<decimal>? scalars) => (decimal)StandardDeviation(scalars?.Select(Convert.ToDouble));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GeometricMean(this IEnumerable<float> scalars) => (float)GeometricMean(scalars.Select(Convert.ToDouble));

        public static double GeometricMean(this IEnumerable<double> scalars)
        {
            double[] arr = scalars as double[] ?? scalars.ToArray();

            return Math.Pow(arr.Product(), 1d / arr.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GeometricMean(this IEnumerable<decimal> scalars) => (decimal)GeometricMean(scalars.Select(Convert.ToDouble));

        /// <summary>
        /// aka. GMDN.
        /// <para/>
        /// See XKCD №2435: <see href="https://xkcd.com/2435/"/>
        /// </summary>
        public static double GeothmeticMeandian(this IEnumerable<double> scalars, double epsilon = 1e-12, int max_iterations = 10_000)
        {
            static double[] F(double[] arr) => new[] { arr.Average(), arr.GeometricMean(), arr.Median() };

            double[] arr = F(scalars.ToArray());
            double last_avg = 0, diff;

            for (int iter = 0; iter < max_iterations; ++iter)
            {
                last_avg = arr[0];
                arr = F(arr);
                diff = last_avg - arr[0];
                diff /= Abs(arr[0]) > double.Epsilon ? arr[0] : Abs(last_avg) > double.Epsilon ? last_avg : 1;

                if (Abs(diff) < epsilon)
                    break;
            }

            return last_avg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FastPow(this double @base, double exp) => Exp(Log(@base) * exp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float FastUnsafeInverseSqrt(float x)
        {
            float x2 = x * .5f;
            int i = *(int*)&x;

            i = 0x5f375a86 - (i >> 1);
            x = *(float*)&i;
            x *= 1.5f - (x2 * x * x);
            x *= 1.5f - (x2 * x * x);

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float FastUnsafeLog2(float x)
        {
            const int f_one = 0x3f800000; // 1.0f
            const float down = 1.0f / 0x0080000;

            return (*(int*)&x - f_one) * down;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float FastUnsafeExp2(float x)
        {
            const int f_one = 0x3f800000; // 1.0f

            return BitConverter.Int32BitsToSingle(*(int*)&x + f_one);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float FastUnsafePow(float x, float y)
        {
            const int f_one = 0x3f800000; // 1.0f

            return BitConverter.Int32BitsToSingle((int)(y * (*(int*)&x - f_one)) + f_one);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float FastUnsafeSqrt(float x)
        {
            const int f_one = 0x03f80000; // 1.0f >> 1

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float sqrt(float x) => BitConverter.Int32BitsToSingle((*(int*)&x >> 1) + f_one);

            float y = sqrt(x);

            // newton iteration
            y = (y * y + x) / (2 * y);
            y = (y * y + x) / (2 * y);

            return y;
        }

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
        public static unsafe T BitRotateLeft<T>(this T value, int offset) where T : unmanaged => BitRotateLeft(value, offset, sizeof(T));

        public static unsafe T BitRotateLeft<T>(this T value, int offset, int bit_size) where T : unmanaged
        {
            if (bit_size == 0)
                return value;
            else if (bit_size > sizeof(ulong))
                throw new ArgumentException($"The input type {typeof(T)} has a size of {sizeof(T)} bytes, however only {sizeof(ulong)} bytes are supported.", nameof(value));

            offset %= bit_size;

            ulong result = 0UL;

            *(T*)&result = value;
            result = offset switch
            {
                0 => result,
                > 0 => (result << offset) | (result >> (bit_size - offset)),
                < 0 => (result >> -offset) | (result >> (bit_size + offset)),
            };

            return *(T*)&result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T ROL<T>(this T value, int offset) where T : unmanaged => BitRotateLeft(value, offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T ROR<T>(this T value, int offset) where T : unmanaged => ROL(value, -offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] XOR(this byte[] arr1, byte[] arr2)
        {
            byte[] res = new byte[Min(arr1.Length, arr2.Length)];

            Parallel.For(0, res.Length, i => res[i] = (byte)(arr1[i] ^ arr2[i]));

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* ToPointer<T>(this ref T value) where T : unmanaged => (T*)Unsafe.AsPointer(ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T ToRef<T>(T* pointer) where T : unmanaged => ref Unsafe.AsRef<T>(pointer);

        public static unsafe void SetLeastSignificantBit<T>(this ref T value, int bit_position, bool bit) where T : unmanaged => value.SetMostSignificantBit(sizeof(T) - 1 - bit_position, bit);

        public static unsafe void SetMostSignificantBit<T>(this ref T value, int bit_position, bool bit) where T : unmanaged
        {
            if (bit_position < 0 || bit_position >= sizeof(T))
                throw new ArgumentOutOfRangeException(nameof(bit_position));

            byte* ptr = (byte*)value.ToPointer() + bit_position / 8;
            int mask = 1 << (7 - bit_position % 8);

            if (bit)
                *ptr |= (byte)mask;
            else
                *ptr &= (byte)~mask;
        }

        public static unsafe bool GetLeastSignificantBit<T>(this ref T value, int bit_position) where T : unmanaged => value.GetMostSignificantBit(sizeof(T) - 1 - bit_position);

        public static unsafe bool GetMostSignificantBit<T>(this ref T value, int bit_position) where T : unmanaged
        {
            if (bit_position < 0 || bit_position >= sizeof(T))
                throw new ArgumentOutOfRangeException(nameof(bit_position));

            byte* ptr = (byte*)value.ToPointer() + bit_position / 8;
            int mask = 1 << (7 - bit_position % 8);
            int bit = *ptr & mask;

            return bit != 0;
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
