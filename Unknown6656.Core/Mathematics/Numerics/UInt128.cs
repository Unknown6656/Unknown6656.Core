#if !NET7_0_OR_GREATER

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

using static System.Math;

namespace Unknown6656.Mathematics.Numerics;


[Serializable, NativeCppClass, StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct UInt128
    : INumericRing<UInt128>
    , IEquatable<UInt128>
    , IComparable
    , ICloneable
    , IFormattable
    , IConvertible
//  , IScalar<UInt128>
//  , IArithmetic<UInt128>
{
    #region PRIVATE FIELDS

    private readonly ulong _hi;
    private readonly ulong _lo;

    #endregion
    #region STATIC PROPERTIES

    public static UInt128 Zero { get; } = default;

    public static UInt128 One { get; } = 1;

    public static UInt128 MaximumValue { get; } = ~Zero;

    public static UInt128 MinimumValue { get; } = Zero;

    #endregion
    #region INSTANCE PROPERTIES

    public readonly byte BitCount => bit_count(this);

    public readonly ulong PopulationCount => population_count(_hi) + population_count(_lo);

    public readonly ulong BinaryTrailingZeroCount => (_lo == 0) ? trailing_zeros(_hi) + 64 : trailing_zeros(_lo);

    public readonly ulong BinaryLeadingZeroCount => (_hi == 0) ? leading_zeros(_lo) + 64 : leading_zeros(_hi);

    public readonly bool IsZero => Is(Zero);

    public readonly bool IsMaximum => Is(MaximumValue);

    public bool IsOne => Is(One);

    [DebuggerHidden, DebuggerNonUserCode, EditorBrowsable(EditorBrowsableState.Never)]
    UInt128 IGroup<UInt128>.AdditiveInverse => (this as IGroup<UInt128>).Negate();

    public bool IsNonZero => !IsZero;

    #endregion
    #region CONSTRUCTORS

    public UInt128(UInt128* ptr)
        : this(*ptr)
    {
    }

    public UInt128(UInt128 value)
        : this(value._hi, value._lo)
    {
    }

    public UInt128(ulong value)
        : this(0, value)
    {
    }

    public UInt128(ulong high, ulong low)
    {
        _hi = high;
        _lo = low;
    }

    #endregion
    #region INSTANCE METHEODS

    private UInt128 Low(ulong new_value) => (_hi, new_value);

    private UInt128 Low(Func<ulong, ulong> f) => (_hi, f(_lo));

    private UInt128 High(ulong new_value) => (new_value, _lo);

    private readonly UInt128 High(Func<ulong, ulong> f) => (f(_hi), _lo);

    [DebuggerHidden, DebuggerNonUserCode, EditorBrowsable(EditorBrowsableState.Never)]
    readonly UInt128 IGroup<UInt128>.Negate() => MaximumValue.Subtract(this).Increment();

    public readonly UInt128 Abs() => this;

    public readonly UInt128 Min(UInt128 second) => CompareTo(second) <= 0 ? this : second;

    public readonly UInt128 Max(UInt128 second) => CompareTo(second) >= 0 ? this : second;

    public readonly UInt128 Clamp() => Clamp(Zero, One);

    public readonly UInt128 Clamp(UInt128 low, UInt128 high) => Min(high).Max(low);

    public readonly UInt128 Add(in UInt128 second) => Add(this, second);

    public readonly UInt128 Add(params UInt128[] others) => others.Aggregate(this, Add);

    public readonly UInt128 Subtract(in UInt128 second) => Subtract(this, second);

    public readonly UInt128 Subtract(params UInt128[] others) => others.Aggregate(this, Subtract);

    public readonly UInt128 Increment() => Increment(this);

    public readonly UInt128 Decrement() => Decrement(this);

    public readonly UInt128 Not() => Not(this);

    public readonly UInt128 Or(UInt128 second) => Or(this, second);

    public readonly UInt128 And(UInt128 second) => And(this, second);

    public readonly UInt128 Xor(UInt128 second) => Xor(this, second);

    public readonly UInt128 ShiftLeft(ulong second) => ShiftLeft(this, second);

    public readonly UInt128 ShiftRight(ulong second) => ShiftRight(this, second);

    public readonly UInt128 Power(int e)
    {
        if (e < 0)
            throw new ArgumentException("The exponent cannot be smaller than zero.", nameof(e));

        UInt128 r = One;
        UInt128 p = this;

        while (e > 0)
            if ((e & 1) == 1)
            {
                --e;
                r = r.Multiply(p);
            }
            else
            {
                e /= 2;
                p = p.Multiply(p);
            }

        return r;
    }

    public readonly UInt128 Multiply(in UInt128 second) => Multiply(this, second);

    public readonly UInt128 Multiply(params UInt128[] others) => others.Aggregate(this, Multiply);

    public readonly (UInt128 High, UInt128 Low) BigMultiply(UInt128 second) => BigMultiply(this, second);

    public readonly UInt128 Square() => Square(this);

    public readonly (UInt128 High, UInt128 Low) BigSquare() => BigSquare(this);

    public readonly (UInt128 Div, UInt128 Mod) DivideModulus(UInt128 second) => DivideModulus(this, second);

    public readonly UInt128 Divide(UInt128 second) => Divide(this, second);

    public readonly UInt128 Modulus(UInt128 second) => Modulus(this, second);

    public readonly object Clone() => new UInt128(this);

    public readonly int CompareTo(UInt128 other) => Compare(this, other);

    public readonly int CompareTo(object? obj) => CompareTo((UInt128)obj!);

    public readonly bool Is(UInt128 o) => (_hi == o._hi) && (_lo == o._lo);

    public readonly bool IsNot(UInt128 o) => !Is(o);

    public readonly bool Equals(UInt128 other) => Is(other);

    public readonly override bool Equals(object? obj) => obj is UInt128 o && Equals(o);

    public readonly override string ToString() => ToString(10);

    public readonly string ToString(byte @base)
    {
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        if (this == Zero)
            return "0";
        else if ((@base < 2) || (@base >= digits.Length))
            throw new ArgumentException($"Base must be in the range [2, {digits.Length - 1}]", nameof(@base));

        StringBuilder sb = new();
        (UInt128 D, UInt128 M) dm = (this, Zero);

        do
        {
            dm = DivideModulus(dm.D, @base);

            sb.Insert(0, digits[(byte)dm.M._lo]);
        }
        while (dm.D != Zero);

        return sb.ToString();
    }

    public readonly override int GetHashCode() => (_hi ^ _lo).GetHashCode() ^ (_hi >> (int)(_lo % 64)).GetHashCode() ^ (_lo >> (int)(_hi % 64)).GetHashCode();

    public readonly string ToString(string? format, IFormatProvider? _)
    {
        if (string.IsNullOrEmpty(format = format?.Trim()))
            format = "d";

        string res = char.ToLowerInvariant(format[0]) switch
        {
            'd' => ToString(10),
            'x' or 'h' => ToString(16),
            'o' => ToString(8),
            'b' => ToString(2),
            _ => throw new FormatException($"The given format '{format}' is invalid.")
        };

        if (uint.TryParse(format.Substring(1), out uint v))
            res = res.PadLeft((int)v, '0');

        if (char.IsUpper(format[0]))
            res = res.ToUpperInvariant();

        return res;
    }

    public readonly TypeCode GetTypeCode() => TypeCode.Object;

    public readonly bool ToBoolean(IFormatProvider? provider) => this != Zero;

    public readonly char ToChar(IFormatProvider? provider) => (char)ToInt32(provider);

    public readonly sbyte ToSByte(IFormatProvider? provider) => (sbyte)ToByte(provider);

    public readonly byte ToByte(IFormatProvider? provider) => (byte)(ToInt16(provider) & 0xff);

    public readonly short ToInt16(IFormatProvider? provider) => (short)ToUInt16(provider);

    public readonly ushort ToUInt16(IFormatProvider? provider) => (ushort)(ToInt32(provider) & 0xffff);

    public readonly int ToInt32(IFormatProvider? provider) => (int)ToUInt32(provider);

    public readonly uint ToUInt32(IFormatProvider? provider) => (uint)(ToUInt64(provider) & 0xffffffffful);

    public readonly long ToInt64(IFormatProvider? provider) => (long)ToUInt64(provider);

    public readonly ulong ToUInt64(IFormatProvider? provider) => (ulong)this;

    public readonly float ToSingle(IFormatProvider? provider) => (float)ToDecimal(provider);

    public readonly double ToDouble(IFormatProvider? provider) => (double)ToDecimal(provider);

    public readonly decimal ToDecimal(IFormatProvider? provider) => this;

    public readonly DateTime ToDateTime(IFormatProvider? provider) => new(ToInt64(provider));

    public readonly string ToString(IFormatProvider? _) => ToString();

    public readonly object ToType(Type conversionType, IFormatProvider? provider)
    {
        try
        {
            return Activator.CreateInstance(conversionType) switch
            {
                char _ => ToChar(provider) as object,
                byte _ => ToByte(provider),
                sbyte _ => ToSByte(provider),
                short _ => ToInt16(provider),
                ushort _ => ToUInt16(provider),
                int _ => ToInt32(provider),
                uint _ => ToUInt32(provider),
                long _ => ToInt64(provider),
                ulong _ => ToUInt64(provider),
                decimal _ => ToDecimal(provider),
                float _ => ToSingle(provider),
                double _ => ToDouble(provider),
                DateTime _ => ToDateTime(provider),
                _ => throw null!,
            };
        }
        catch
        {
            throw new InvalidCastException($"An instance of '{typeof(UInt128)} cannot be converted to '");
        }
    }

    #endregion
    #region STATIC METHODS

    public static int Compare(UInt128 x, UInt128 y) =>
        (((x._hi > y._hi) || ((x._hi == y._hi) && (x._lo > y._lo))) ? 1 : 0)
      - (((x._hi < y._hi) || ((x._hi == y._hi) && (x._lo < y._lo))) ? 1 : 0);

    public static UInt128 Add(UInt128 x, UInt128 y)
    {
        ulong C = (((x._lo & y._lo) & 1) + (x._lo >> 1) + (y._lo >> 1)) >> 63;

        return (
            x._hi + y._hi + C,
            x._lo + y._lo
        );
    }

    public static UInt128 Subtract(UInt128 x, UInt128 y)
    {
        UInt128 res = x._lo - y._lo;
        ulong C = (((res._lo & y._lo) & 1) + (y._lo >> 1) + (res._lo >> 1)) >> 63;

        return res.High(x._hi - (y._hi + C));
    }

    public static UInt128 Increment(UInt128 x)
    {
        ulong T = x._lo + 1;

        return (x._hi + ((x._lo ^ T) & x._lo) >> 63, T);
    }

    public static UInt128 Decrement(UInt128 x)
    {
        ulong T = x._lo - 1;

        return (x._hi - ((T ^ x._lo) & T) >> 63, T);
    }

    public static UInt128 Not(UInt128 x) => (~x._hi, ~x._lo);

    public static UInt128 Or(UInt128 x, UInt128 y) => (x._hi | y._hi, x._lo | y._lo);

    public static UInt128 And(UInt128 x, UInt128 y) => (x._hi & y._hi, x._lo & y._lo);

    public static UInt128 Xor(UInt128 x, UInt128 y) => (x._hi ^ y._hi, x._lo ^ y._lo);

    public static UInt128 ShiftLeft(UInt128 x, ulong y)
    {
        int iy = (int)y;

        y &= 127;

        ulong m1 = ((((y + 127) | y) & 64) >> 6) - 1;
        ulong m2 = (y >> 6) - 1;

        y &= 63; // TODO : iy?

        ulong h = (x._lo << iy) & ~m2;
        ulong l = (x._lo << iy) & m2;

        h |= ((x._hi << iy) | ((x._lo >> (64 - iy)) & m1)) & m2;

        return (h, l);
    }

    public static UInt128 ShiftRight(UInt128 x, ulong y)
    {
        int iy = (int)y;

        y &= 127;

        ulong m1 = ((((y + 127) | y) & 64) >> 6) - 1;
        ulong m2 = (y >> 6) - 1;

        y &= 63; // TODO : iy?

        ulong l = (x._hi >> iy) & ~m2;
        ulong h = (x._hi >> iy) & m2;

        l |= ((x._lo >> iy) | ((x._hi << (64 - iy)) & m1)) & m2;

        return (h, l);
    }

    public static UInt128 Multiply(ulong x, ulong y)
    {
        if (x == y)
            return Square(x);

        ulong u1 = x & 0xffffffff;
        ulong v1 = y & 0xffffffff;
        ulong t = u1 * v1;
        ulong w3 = t & 0xffffffff;
        ulong k = t >> 32;

        x >>= 32;
        t = (x * v1) + k;
        k = t & 0xffffffff;

        ulong w1 = t >> 32;

        y >>= 32;
        t = (u1 * y) + k;
        k = t >> 32;

        return ((x * y) + w1 + k, (t << 32) + w3);
    }

    public static UInt128 Multiply(UInt128 x, UInt128 y) => x == y ? Square(x) : Multiply(x._lo, y._lo).High(h => h + (x._hi * y._lo) + (x._lo * y._hi));

    public static (UInt128 High, UInt128 Low) BigMultiply(UInt128 x, UInt128 y)
    {
        if (x == y)
            return BigSquare(x);

        UInt128 H = Multiply(x._hi, y._hi);
        UInt128 L = Multiply(x._lo, y._lo);
        UInt128 T = Multiply(x._hi, y._lo);

        L = L.High(h => h + T._lo);

        if (L._hi < T._lo)  // if L.Hi overflowed
            ++H;

         H = (H._hi, H._lo + T._hi);

        if (H._lo < T._hi)  // if H.Lo overflowed
            H = H.High(h => ++h);

        T = Multiply(x._lo, y._hi);

        L = (L._hi + T._lo, L._lo);

        if (L._hi < T._lo)  // if L.Hi overflowed
           ++H;

        H = H.Low(l => l + T._hi);

        if (H._lo < T._hi)  // if H.Lo overflowed
            H = H.Low(l => ++l);

        return (H, T);
    }

    public static UInt128 Square(ulong x)
    {
        ulong r1 = x & 0xffffffff;
        ulong t = r1 * r1;
        ulong w3 = t & 0xffffffff;
        ulong k = t >> 32;

        x >>= 32;

        ulong m = x * r1;

        t = m + k;

        ulong w2 = t & 0xffffffff;
        ulong w1 = t >> 32;

        t = m + w2;
        k = t >> 32;

        return ((x * x) + w1 + k, (t << 32) + w3);
    }

    public static UInt128 Square(UInt128 x)
    {
        UInt128 res = Square(x._lo);

        return (res._hi + ((res._hi * res._lo) << 1), res._lo);
    }

    public static (UInt128 High, UInt128 Low) BigSquare(UInt128 R)
    {
        UInt128 H = Square(R._hi);
        UInt128 L = Square(R._lo);
        UInt128 T = Square((R._hi, R._lo));

        H = H.High(h => h + (T._hi >> 63));
        T = ((T._hi << 1) | (T._lo >> 63), T._lo << 1);
        L = L.High(h => h + T._lo);

        if (L._hi < T._lo)  // if L.Hi overflowed
            ++H;

        H.Low(l => l + T._hi);

        if (H._lo < T._hi)  // if H.Lo overflowed
            ++H;

        return (H, L);
    }

    public static (UInt128 Div, UInt128 Mod) DivideModulus(UInt128 x, UInt128 y)
    {
        if (y == Zero)
            throw new DivideByZeroException();
        else if (x == 1)
            return (x, Zero);
        else if (x == y)
            return (1, Zero);
        else if ((x == Zero) || (x < y))
            return (Zero, x);

        var res = (H: Zero, L: Zero);

        for (byte b = bit_count(x); b > 0; --b)
        {
            res.H <<= 1;
            res.L <<= 1;

            if (((x >> (b - 1)) & 1) != Zero)
                ++res.L;

            if (res.L >= y)
            {
                res.L -= y;
                res.H++;
            }
        }

        return res;

    }

    public static UInt128 Divide(UInt128 x, UInt128 y) => DivideModulus(x, y).Div;

    public static UInt128 Modulus(UInt128 x, UInt128 y) => DivideModulus(x, y).Mod;

    public static UInt128 GreatestCommonDivisor(UInt128 a, UInt128 b) =>
        // TODO : extended euler algorithm

        throw new NotImplementedException();

    private static byte bit_count(UInt128 v)
    {
        ulong low = v._lo;
        ulong up = v._hi;
        byte res = 0;

        if (v._hi != 0)
        {
            res = 64;

            while (up != 0)
            {
                up >>= 1;
                res++;
            }
        }
        else
            while (low != 0)
            {
                low >>= 1;
                res++;
            }

        return res;
    }

    private static ulong population_count(ulong x)
    {
        x -= (x >> 1) & 0x5555555555555555;
        x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);

        return ((x + (x >> 4) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56;
    }

    private static ulong trailing_zeros(ulong x)
    {
        ulong I = ~x;
        ulong res = ((I ^ (I + 1)) & I) >> 63;
        int ii = (int)I;

        I = (x & 0xffffffff) + 0xffffffff;
        I = ((I & 0x100000000) ^ 0x100000000) >> 27;
        x >>= ii;
        res += I;

        I = (x & 0xffff) + 0xffff;
        I = ((I & 0x10000) ^ 0x10000) >> 12;
        x >>= ii;
        res += I;

        I = (x & 0xff) + 0xff;
        I = ((I & 0x100) ^ 0x100) >> 5;
        x >>= ii;
        res += I;

        I = (x & 0xf) + 0xf;
        I = ((I & 0x10) ^ 0x10) >> 2;
        x >>= ii;
        res += I;

        I = (x & 3) + 3;
        I = ((I & 4) ^ 4) >> 1;
        x >>= ii;
        res += I;

        res += ((x & 1) ^ 1);

        return res;
    }

    private static ulong leading_zeros(ulong x)
    {
        ulong I = ~x;
        ulong res = ((I ^ (I + 1)) & I) >> 63;
        int ii = (int)I;

        I = (x >> 32) + 0xffffffff;
        I = ((I & 0x100000000) ^ 0x100000000) >> 27;
        res += I;
        x <<= ii;

        I = (x >> 48) + 0xffff;
        I = ((I & 0x10000) ^ 0x10000) >> 12;
        res += I;
        x <<= ii;

        I = (x >> 56) + 0xff;
        I = ((I & 0x100) ^ 0x100) >> 5;
        res += I;
        x <<= ii;

        I = (x >> 60) + 0xf;
        I = ((I & 0x10) ^ 0x10) >> 2;
        res += I;
        x <<= ii;

        I = (x >> 62) + 3;
        I = ((I & 4) ^ 4) >> 1;
        res += I;
        x <<= ii;

        res += (x >> 63) ^ 1;

        return res;
    }

    #endregion
    #region OPERATORS

    public static UInt128 operator +(in UInt128 x) => x;

    public static UInt128 operator ~(in UInt128 x) => Not(x);

    public static UInt128 operator -(in UInt128 x) => Subtract(Zero, x);

    public static UInt128 operator ++(in UInt128 x) => Increment(x);

    public static UInt128 operator --(in UInt128 x) => Decrement(x);

    public static UInt128 operator +(in UInt128 x, in UInt128 y) => Add(x, y);

    public static UInt128 operator -(in UInt128 x, in UInt128 y) => Subtract(x, y);

    public static UInt128 operator *(in UInt128 x, in UInt128 y) => Multiply(x, y);

    public static UInt128 operator /(in UInt128 x, in UInt128 y) => Divide(x, y);

    public static UInt128 operator %(in UInt128 x, in UInt128 y) => Modulus(x, y);

    public static UInt128 operator ^(UInt128 x, UInt128 y) => Xor(x, y);

    public static UInt128 operator |(UInt128 x, UInt128 y) => Or(x, y);

    public static UInt128 operator &(UInt128 x, UInt128 y) => And(x, y);

    public static UInt128 operator <<(UInt128 x, int y) => ShiftLeft(x, (ulong)y);

    public static UInt128 operator >>(UInt128 x, int y) => ShiftRight(x, (ulong)y);

    public static bool operator ==(UInt128 x, UInt128 y) => x.Equals(y);

    public static bool operator !=(UInt128 x, UInt128 y) => !(x == y);

    public static bool operator <=(UInt128 x, UInt128 y) => x.CompareTo(y) <= 0;

    public static bool operator >=(UInt128 x, UInt128 y) => x.CompareTo(y) >= 0;

    public static bool operator <(UInt128 x, UInt128 y) => x.CompareTo(y) == -1;

    public static bool operator >(UInt128 x, UInt128 y) => x.CompareTo(y) == 1;

    public static explicit operator bool(UInt128 v) => !v.IsZero;

    public static implicit operator UInt128(ulong v) => new(v);

    public static implicit operator UInt128((ulong High, ulong Low) v) => new(v.High, v.Low);

    public static implicit operator (ulong High, ulong Low)(UInt128 v) => (v._hi, v._lo);

    public static explicit operator UInt128(decimal v)
    {
        UInt128 res = Zero;
        UInt128 btm = 1;

        v = Floor(v);

        while (v != 0m)
        {
            if (v % 2 != 0)
                res |= btm;

            btm <<= 1;
            v = Floor(v / 2);
        }

        return res;
    }

    public static implicit operator decimal(UInt128 v) => decimal.Parse(v.ToString());

    public static implicit operator BigInteger(UInt128 v) => new(v);

    public static explicit operator ulong(UInt128 v) => v._lo;

    #endregion
}

#endif