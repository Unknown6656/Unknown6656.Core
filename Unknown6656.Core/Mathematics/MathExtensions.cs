using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;
using Unknown6656.Common;

using static System.Math;

namespace Unknown6656.Mathematics;


public static partial class MathExtensions
{
    private const string BASE_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const decimal M_PI = 3.14159265358979323846m;


    public static T BinaryToGray<T>(this T num)
        where T : num.IBitwiseOperators<T, T, T>
                , num.IShiftOperators<T, int, T> => num ^ (num >> 1);

    public static T GrayToBinary<T>(this T num)
        where T : num.IBitwiseOperators<T, T, T>
                , num.IShiftOperators<T, int, T>
                , num.IEqualityOperators<T, T, bool>
    {
        T mask = num;

        while (mask != default)
        {
            mask >>= 1;
            num ^= mask;
        }

        return num;
    }

    public static int[] GetOffsets(this Range range, int length, int offset = 0)
    {
        int start = range.Start.GetOffset(length) + offset;
        int end = range.End.GetOffset(length) + offset;
        int[] arr = new int[end - start];

        for (int i = 0; i < arr.Length; ++i)
            arr[i] = start + i;

        return arr;
    }

    public static bool IsPowerOf2<T>(this T l)
        where T : struct
                , num.IBitwiseOperators<T, T, T>
                , num.IEqualityOperators<T, T, bool>
                , num.IDecrementOperators<T>
    {
        T c = l;

        --c;

        return (c & l) == default;
    }

    public static bool IsZero(this double v) => Abs(v) <= 2 * double.Epsilon;

    public static bool Is(this double x, double y) => Scalar.Is(x, y);

    public static bool Are(this IEnumerable<double> xs, IEnumerable<double> ys) => xs.Are(ys, Scalar.EqualityComparer);

    public static bool Are(this IEnumerable<Scalar> xs, IEnumerable<Scalar> ys) => xs.Are(ys, Scalar.EqualityComparer);

    public static bool Are(this IEnumerable<float> xs, IEnumerable<float> ys) => xs.Are(ys, Scalar.EqualityComparer);

    public static double Clamp(this double x) => x.Clamp(0, 1);

    public static T Clamp<T>(this T scalar, T min, T max) where T : num.IComparisonOperators<T, T, bool> => scalar <= min ? min : scalar >= max ? max : scalar;

    public static T Map<T>(this T scalar, (T lower, T upper) from, (T lower, T upper) to)
        where T : num.IDivisionOperators<T, T, T>
                , num.ISubtractionOperators<T, T, T>
                , num.IMultiplyOperators<T, T, T>
                , num.IAdditionOperators<T, T, T> => (scalar - from.lower) / (from.upper - from.lower) * (to.upper - to.lower) + to.lower;

    public static T ClampMap<T>(this T scalar, (T lower, T upper) from, (T lower, T upper) to)
        where T : num.IDivisionOperators<T, T, T>
                , num.ISubtractionOperators<T, T, T>
                , num.IMultiplyOperators<T, T, T>
                , num.IAdditionOperators<T, T, T>
                , num.IComparisonOperators<T, T, bool> => scalar.Clamp(from.lower, from.upper).Map(from, to);


    public static float Product(this IEnumerable<float> scalars) => scalars.Aggregate(1f, (s1, s2) => s1 * s2);

    public static double Product(this IEnumerable<double> scalars) => scalars.Aggregate(1d, (s1, s2) => s1 * s2);

    public static decimal Product(this IEnumerable<decimal> scalars) => scalars.Aggregate(1m, (s1, s2) => s1 * s2);

    [return: MaybeNull]
    public static T Median<T>(this IEnumerable<T> scalars)
        where T : IComparable<T>
    {
        T[] ordered = [.. scalars.OrderBy(LINQ.id)];

        return ordered.Length == 0 ? default : ordered[ordered.Length / 2];
    }

    public static float Average(this IEnumerable<float>? scalars) => (scalars as float[] ?? scalars?.ToArray()) is float[] arr ? arr.Sum() / arr.Length : 0f;

    public static double Average(this IEnumerable<double>? scalars) => (scalars as double[] ?? scalars?.ToArray()) is double[] arr ? arr.Sum() / arr.Length : 0d;

    public static decimal Average(this IEnumerable<decimal>? scalars) => (scalars as decimal[] ?? scalars?.ToArray()) is decimal[] arr ? arr.Sum() / arr.Length : 0m;

    public static float Variance(this IEnumerable<float>? scalars) => (float)Variance(scalars?.Select(Convert.ToDouble));

    public static double Variance(this IEnumerable<double>? scalars)
    {
        double avg = scalars.Average();

        return Pow(scalars?.Sum(x => x - avg) ?? 0, 2);
    }

    public static float StandardDeviation(this IEnumerable<float>? scalars) => (float)StandardDeviation(scalars?.Select(Convert.ToDouble));

    public static double StandardDeviation(this IEnumerable<double>? scalars) => Sqrt(scalars.Variance());

    public static decimal StandardDeviation(this IEnumerable<decimal>? scalars) => (decimal)StandardDeviation(scalars?.Select(Convert.ToDouble));

    public static float GeometricMean(this IEnumerable<float> scalars) => (float)GeometricMean(scalars.Select(Convert.ToDouble));

    public static double GeometricMean(this IEnumerable<double> scalars)
    {
        double[] arr = scalars as double[] ?? scalars.ToArray();

        return Pow(arr.Product(), 1d / arr.Length);
    }

    public static decimal GeometricMean(this IEnumerable<decimal> scalars) => (decimal)GeometricMean(scalars.Select(Convert.ToDouble));

    /// <summary>
    /// aka. GMDN.
    /// <para/>
    /// See XKCD №2435: <see href="https://xkcd.com/2435/"/>
    /// </summary>
    public static double GeothmeticMeandian(this IEnumerable<double> scalars, double epsilon = 1e-12, int max_iterations = 10_000)
    {
        double[] arr = [scalars.Average(), scalars.GeometricMean(), scalars.Median()];
        double last_avg = 0, diff;

        for (int iter = 0; iter < max_iterations; ++iter)
        {
            (last_avg, arr[0], arr[1], arr[2]) = (arr[0], arr.Average(), arr.GeometricMean(), arr.Median());
            diff = last_avg - arr[0];
            diff /= Abs(arr[0]) > double.Epsilon ? arr[0] : Abs(last_avg) > double.Epsilon ? last_avg : 1;

            if (Abs(diff) < epsilon)
                break;
        }

        return last_avg;
    }

    public static double FastPow(this double @base, double exp) => Exp(Log(@base) * exp);

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

    public static unsafe float FastUnsafeLog2(float x)
    {
        const int f_one = 0x3f800000; // 1.0f
        const float down = 1.0f / 0x0080000;

        return (*(int*)&x - f_one) * down;
    }

    public static unsafe float FastUnsafeExp2(float x)
    {
        const int f_one = 0x3f800000; // 1.0f

        return BitConverter.Int32BitsToSingle(*(int*)&x + f_one);
    }

    public static unsafe float FastUnsafePow(float x, float y)
    {
        const int f_one = 0x3f800000; // 1.0f

        return BitConverter.Int32BitsToSingle((int)(y * (*(int*)&x - f_one)) + f_one);
    }

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

    public static double ACosh(this double x) => x < 1 ? 0 : Log(x + Sqrt(x * x - 1));

    public static double ASinh(this double x) => (x < 0 ? -1 : x > 0 ? 1 : 0) * Log(Abs(x) + Sqrt(1 + x * x));

    public static double ATanh(this double x) => Abs(x) >= 1 ? 0 : .5f * Log((1 + x) / (1 - x));

    public static double Degrees(this double radians) => radians * 57.295779513082320876798154814105;

    public static Scalar Degrees(this Scalar radians) => radians * 57.295779513082320876798154814105;

    public static double Radians(this double degrees) => degrees * 0.01745329251994329576923690768489;

    public static Scalar Radians(this Scalar degrees) => degrees * 0.01745329251994329576923690768489;

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

    public static unsafe bool HasFlag<T>(this T value, T flag)
        where T : unmanaged
    {
        byte* ptr1 = (byte*)&value;
        byte* ptr2 = (byte*)&flag;

        for (int i = 0; i < sizeof(T); ++i)
            if ((ptr1[i] & ptr2[i]) != 0)
                return true;

        return false;
    }

    public static unsafe T ROL<T>(this T value, int offset) where T : unmanaged => BitRotateLeft(value, offset);

    public static unsafe T ROR<T>(this T value, int offset) where T : unmanaged => ROL(value, -offset);

    public static byte[] XOR(this byte[] arr1, byte[] arr2)
    {
        byte[] res = new byte[Min(arr1.Length, arr2.Length)];

        Parallel.For(0, res.Length, i => res[i] = (byte)(arr1[i] ^ arr2[i]));

        return res;
    }

    public static unsafe T* ToPointer<T>(this ref T value) where T : unmanaged => (T*)Unsafe.AsPointer(ref value);

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

    public static ulong GreatestCommonDivisor(ulong a, ulong b)
    {
        while (a != 0 && b != 0)
            if (a > b)
                a %= b;
            else
                b %= a;

        return a == 0 ? b : a;
    }

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

    public static bint LeastCommonMultiple(bint a, bint b) => (a / GreatestCommonDivisor(a, b)) * b;

    public static ulong LeastCommonMultiple(ulong a, ulong b) => (a / GreatestCommonDivisor(a, b)) * b;

    public static bool IsPrime(this bint number)
    {
        if (number < 2)
            return false;

        int limit = _primes[^1];

        if (number <= limit)
            return Array.BinarySearch(_primes, number) >= 0;

        return !Enumerable.Range(limit + 1, (int)Sqrt((double)number) - limit).AsParallel().Select(i => number % i).Any(i => i == 0);
    }

    public static int BinomialCoefficient(int n, int k) => (int)BinomialCoefficient((bint)n, k);

    public static long BinomialCoefficient(long n, long k) => (long)BinomialCoefficient((bint)n, k);

    public static bint BinomialCoefficient(bint n, bint k)
    {
        bint result = 1;

        for (bint i = 0; i < k; ++i)
            result *= (n - i) / (i + 1);

        return result;
    }

    public static bint[] PrimeFactorization(this bint a)
    {
        List<bint> f = [];

        for (bint b = 2; a > 1; ++b)
            while (a % b == 0)
            {
                a /= b;
                f.Add(b);
            }

        return [.. f];
    }

    public static bint? Phi(this bint a) => a.PrimeFactorization().ToArray() is { Length: 2 } l ? (bint?)((l[0] - 1) * (l[1] - 1)) : null;

    public static void InterlockedMinMax(ref double min, ref double max, double value)
    {
        double init;

        do
        {
            init = min;

            if (init <= value)
                break;
        }
        while (Interlocked.CompareExchange(ref min, value, init) != init);

        do
        {
            init = max;

            if (init >= value)
                return;
        }
        while (Interlocked.CompareExchange(ref max, value, init) != init);
    }

    #region ERF(X) / ERFC(X)

    private const double ε = 1e-300;
    private const double erx = 8.45062911510467529297e-01;

    // Coefficients for approximation to erf on [0, 0.84375]
    private const double efx = 1.28379167095512586316e-01; /* 0x3FC06EBA; 0x8214DB69 */
    private const double efx8 = 1.02703333676410069053e+00; /* 0x3FF06EBA; 0x8214DB69 */
    private const double pp0 = 1.28379167095512558561e-01; /* 0x3FC06EBA; 0x8214DB68 */
    private const double pp1 = -3.25042107247001499370e-01; /* 0xBFD4CD7D; 0x691CB913 */
    private const double pp2 = -2.84817495755985104766e-02; /* 0xBF9D2A51; 0xDBD7194F */
    private const double pp3 = -5.77027029648944159157e-03; /* 0xBF77A291; 0x236668E4 */
    private const double pp4 = -2.37630166566501626084e-05; /* 0xBEF8EAD6; 0x120016AC */
    private const double qq1 = 3.97917223959155352819e-01; /* 0x3FD97779; 0xCDDADC09 */
    private const double qq2 = 6.50222499887672944485e-02; /* 0x3FB0A54C; 0x5536CEBA */
    private const double qq3 = 5.08130628187576562776e-03; /* 0x3F74D022; 0xC4D36B0F */
    private const double qq4 = 1.32494738004321644526e-04; /* 0x3F215DC9; 0x221C1A10 */
    private const double qq5 = -3.96022827877536812320e-06; /* 0xBED09C43; 0x42A26120 */

    // Coefficients for approximation to erf in [0.84375, 1.25]
    private const double pa0 = -2.36211856075265944077e-03; /* 0xBF6359B8; 0xBEF77538 */
    private const double pa1 = 4.14856118683748331666e-01; /* 0x3FDA8D00; 0xAD92B34D */
    private const double pa2 = -3.72207876035701323847e-01; /* 0xBFD7D240; 0xFBB8C3F1 */
    private const double pa3 = 3.18346619901161753674e-01; /* 0x3FD45FCA; 0x805120E4 */
    private const double pa4 = -1.10894694282396677476e-01; /* 0xBFBC6398; 0x3D3E28EC */
    private const double pa5 = 3.54783043256182359371e-02; /* 0x3FA22A36; 0x599795EB */
    private const double pa6 = -2.16637559486879084300e-03; /* 0xBF61BF38; 0x0A96073F */
    private const double qa1 = 1.06420880400844228286e-01; /* 0x3FBB3E66; 0x18EEE323 */
    private const double qa2 = 5.40397917702171048937e-01; /* 0x3FE14AF0; 0x92EB6F33 */
    private const double qa3 = 7.18286544141962662868e-02; /* 0x3FB2635C; 0xD99FE9A7 */
    private const double qa4 = 1.26171219808761642112e-01; /* 0x3FC02660; 0xE763351F */
    private const double qa5 = 1.36370839120290507362e-02; /* 0x3F8BEDC2; 0x6B51DD1C */
    private const double qa6 = 1.19844998467991074170e-02; /* 0x3F888B54; 0x5735151D */

    // Coefficients for approximation to erfc in [1.25, 1/0.35]
    private const double ra0 = -9.86494403484714822705e-03; /* 0xBF843412; 0x600D6435 */
    private const double ra1 = -6.93858572707181764372e-01; /* 0xBFE63416; 0xE4BA7360 */
    private const double ra2 = -1.05586262253232909814e+01; /* 0xC0251E04; 0x41B0E726 */
    private const double ra3 = -6.23753324503260060396e+01; /* 0xC04F300A; 0xE4CBA38D */
    private const double ra4 = -1.62396669462573470355e+02; /* 0xC0644CB1; 0x84282266 */
    private const double ra5 = -1.84605092906711035994e+02; /* 0xC067135C; 0xEBCCABB2 */
    private const double ra6 = -8.12874355063065934246e+01; /* 0xC0545265; 0x57E4D2F2 */
    private const double ra7 = -9.81432934416914548592e+00; /* 0xC023A0EF; 0xC69AC25C */
    private const double sa1 = 1.96512716674392571292e+01; /* 0x4033A6B9; 0xBD707687 */
    private const double sa2 = 1.37657754143519042600e+02; /* 0x4061350C; 0x526AE721 */
    private const double sa3 = 4.34565877475229228821e+02; /* 0x407B290D; 0xD58A1A71 */
    private const double sa4 = 6.45387271733267880336e+02; /* 0x40842B19; 0x21EC2868 */
    private const double sa5 = 4.29008140027567833386e+02; /* 0x407AD021; 0x57700314 */
    private const double sa6 = 1.08635005541779435134e+02; /* 0x405B28A3; 0xEE48AE2C */
    private const double sa7 = 6.57024977031928170135e+00; /* 0x401A47EF; 0x8E484A93 */
    private const double sa8 = -6.04244152148580987438e-02; /* 0xBFAEEFF2; 0xEE749A62 */

    // Coefficients for approximation to erfc in [1/0.35, 28]
    private const double rb0 = -9.86494292470009928597e-03; /* 0xBF843412; 0x39E86F4A */
    private const double rb1 = -7.99283237680523006574e-01; /* 0xBFE993BA; 0x70C285DE */
    private const double rb2 = -1.77579549177547519889e+01; /* 0xC031C209; 0x555F995A */
    private const double rb3 = -1.60636384855821916062e+02; /* 0xC064145D; 0x43C5ED98 */
    private const double rb4 = -6.37566443368389627722e+02; /* 0xC083EC88; 0x1375F228 */
    private const double rb5 = -1.02509513161107724954e+03; /* 0xC0900461; 0x6A2E5992 */
    private const double rb6 = -4.83519191608651397019e+02; /* 0xC07E384E; 0x9BDC383F */
    private const double sb1 = 3.03380607434824582924e+01; /* 0x403E568B; 0x261D5190 */
    private const double sb2 = 3.25792512996573918826e+02; /* 0x40745CAE; 0x221B9F0A */
    private const double sb3 = 1.53672958608443695994e+03; /* 0x409802EB; 0x189D5118 */
    private const double sb4 = 3.19985821950859553908e+03; /* 0x40A8FFB7; 0x688C246A */
    private const double sb5 = 2.55305040643316442583e+03; /* 0x40A3F219; 0xCEDF3BE6 */
    private const double sb6 = 4.74528541206955367215e+02; /* 0x407DA874; 0xE79FE763 */
    private const double sb7 = -2.24409524465858183362e+01; /* 0xC03670E2; 0x42712D62 */

    /// <summary>
    /// Returns the value of the gaussian error function at <paramref name="x"/>.
    /// </summary>
    public static unsafe double Erf(double x)
    {
        if (double.IsNaN(x))
            return double.NaN;
        else if (double.IsNegativeInfinity(x))
            return -1.0;
        else if (double.IsPositiveInfinity(x))
            return 1.0;

        int n0, hx, ix, i;
        double R, S, P, Q, s, y, z, r;
        double one = 1.0;

        n0 = ((*(int*)&one) >> 29) ^ 1;
        hx = *(n0 + (int*)&x);
        ix = hx & 0x7FFFFFFF;

        if (ix < 0x3FEB0000) // |x| < 0.84375
        {
            if (ix < 0x3E300000) // |x| < 2**-28
                return ix < 0x00800000 ? 0.125 * (8.0 * x + efx8 * x) /* avoid underflow */ : x + efx * x;

            z = x * x;
            r = pp0 + z * (pp1 + z * (pp2 + z * (pp3 + z * pp4)));
            s = 1.0 + z * (qq1 + z * (qq2 + z * (qq3 + z * (qq4 + z * qq5))));
            y = r / s;

            return x + x * y;
        }
        else if (ix < 0x3FF40000) // 0.84375 <= |x| < 1.25
        {
            s = Abs(x) - 1.0;
            P = pa0 + s * (pa1 + s * (pa2 + s * (pa3 + s * (pa4 + s * (pa5 + s * pa6)))));
            Q = 1.0 + s * (qa1 + s * (qa2 + s * (qa3 + s * (qa4 + s * (qa5 + s * qa6)))));

            return hx >= 0 ? erx + P / Q : -erx - P / Q;
        }
        else if (ix >= 0x40180000) // inf > |x| >= 6
            return hx >= 0 ? 1.0 - ε : ε - 1.0;

        x = Abs(x);
        s = 1 / (x * x);

        if (ix < 0x4006DB6E) // |x| < 1/0.35
        {
            R = ra0 + s * (ra1 + s * (ra2 + s * (ra3 + s * (ra4 + s * (ra5 + s * (ra6 + s * ra7))))));
            S = 1.0 + s * (sa1 + s * (sa2 + s * (sa3 + s * (sa4 + s * (sa5 + s * (sa6 + s * (sa7 + s * sa8)))))));
        }
        else // |x| >= 1/0.35
        {
            R = rb0 + s * (rb1 + s * (rb2 + s * (rb3 + s * (rb4 + s * (rb5 + s * rb6)))));
            S = 1.0 + s * (sb1 + s * (sb2 + s * (sb3 + s * (sb4 + s * (sb5 + s * (sb6 + s * sb7))))));
        }

        z = x;
        *(1 - n0 + (int*)&z) = 0;
        r = Exp(-z * z - 0.5625) * Exp((z - x) * (z + x) + R / S);

        return hx >= 0 ? 1.0 - r / x : r / x - 1.0;
    }

    /// <summary>
    /// Returns the value of the complementary error function at <paramref name="x"/>.
    /// </summary>
    public static unsafe double Erfc(double x)
    {
        if (double.IsNaN(x))
            return double.NaN;
        else if (double.IsNegativeInfinity(x))
            return 2;
        else if (double.IsPositiveInfinity(x))
            return 0;

        int n0, hx, ix;
        double R, S, P, Q, s, y, z, r;
        double one = 1;

        n0 = ((*(int*)&one) >> 29) ^ 1;
        hx = *(n0 + (int*)&x);
        ix = hx & 0x7FFFFFFF;

        if (ix < 0x3FEB0000) // |x| < 0.84375
        {
            if (ix < 0x3C700000) // |x| < 2**-56
                return 1.0 - x;

            z = x * x;
            r = pp0 + z * (pp1 + z * (pp2 + z * (pp3 + z * pp4)));
            s = 1.0 + z * (qq1 + z * (qq2 + z * (qq3 + z * (qq4 + z * qq5))));
            y = r / s;
            
            if (hx < 0x3FD00000) // x < 1/4
                return 1 - (x + x * y);

            r = x * y;
            r += x - .5;

            return .5 - r;
        }
        else if (ix < 0x3FF40000) // 0.84375 <= |x| < 1.25
        {
            s = Abs(x) - 1.0;
            P = pa0 + s * (pa1 + s * (pa2 + s * (pa3 + s * (pa4 + s * (pa5 + s * pa6)))));
            Q = 1.0 + s * (qa1 + s * (qa2 + s * (qa3 + s * (qa4 + s * (qa5 + s * qa6)))));

            return hx >= 0 ? 1 - erx - P / Q : 1 + erx + P / Q;
        }
        else if (ix < 0x403C0000) // |x| < 28
        {
            x = Abs(x);
            s = 1 / (x * x);

            if (ix < 0x4006DB6D) // |x| < 1/.35 ~ 2.857143
            {
                R = ra0 + s * (ra1 + s * (ra2 + s * (ra3 + s * (ra4 + s * (ra5 + s * (ra6 + s * ra7))))));
                S = 1.0 + s * (sa1 + s * (sa2 + s * (sa3 + s * (sa4 + s * (sa5 + s * (sa6 + s * (sa7 + s * sa8)))))));
            }
            else // |x| >= 1/.35 ~ 2.857143
            {
                if (hx < 0 && ix >= 0x40180000)
                    return 2.0 - ε; // x < -6

                R = rb0 + s * (rb1 + s * (rb2 + s * (rb3 + s * (rb4 + s * (rb5 + s * rb6)))));
                S = 1.0 + s * (sb1 + s * (sb2 + s * (sb3 + s * (sb4 + s * (sb5 + s * (sb6 + s * sb7))))));
            }

            z = x;
            *(1 - n0 + (int*)&z) = 0;
            r = Exp(-z * z - 0.5625) * Exp((z - x) * (z + x) + R / S);

            return hx > 0 ? r / x : 2 - r / x;
        }
        else
            return hx > 0 ? ε * ε : 2 - ε;
    }

    #endregion

    public static string LongToBase(long value, int target_base)
    {
        string @base = BASE_CHARS[..target_base];
        char[] buffer = new char[Max((int)Ceiling(Log(value + 1, target_base)), 1)];
        long i = buffer.Length;

        do
        {
            buffer[--i] = @base[(int)(value % target_base)];
            value /= target_base;
        }
        while (value > 0);

        return new string(buffer, (int)i, (int)(buffer.Length - i));
    }

    public static long BaseToLong(string number, int source_base)
    {
        string @base = BASE_CHARS[..source_base];
        int m = number.Length - 1;
        int n = source_base, x;
        long result = 0;

        for (int i = 0; i < number.Length; i++)
        {
            x = @base.IndexOf(number[i]);
            result += x * (long)Pow(n, m--);
        }

        return result;
    }

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
        string[] sizes = ["B", "KB", "MB", "GB", "TB", "EB", "PB"];
        int order = 0;

        while (l >= 1024 && order < sizes.Length - 1)
        {
            ++order;

            l /= 1024;
        }

        return $"{l:0.##} {sizes[order]}";
    }
}
