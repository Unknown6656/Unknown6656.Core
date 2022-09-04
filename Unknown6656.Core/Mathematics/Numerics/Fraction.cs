using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Numerics;


public readonly struct Fraction
    : INumericRing<Fraction>
    , IScalar
{
    public static Fraction PositiveInfinity { get; } = new(1, 0);

    public static Fraction NegativeInfinity { get; } = new(-1, 0);

    public static Fraction NaN { get; } = new(0, 0);

    public static Fraction Zero { get; } = new(0, 1);

    public static Fraction One { get; } = new(1, 1);

    public static Fraction NegativeOne { get; } = new(-1, 1);

    public static Fraction Epsilon { get; } = new(1, ulong.MaxValue);


    public readonly long Numerator { get; }

    public readonly ulong Denominator { get; }

    public readonly Scalar AsScalar => this;

    public readonly Fraction AdditiveInverse => Negate();

    public readonly Fraction MultiplicativeInverse => new(Math.Sign(Numerator) * (long)Denominator, (ulong)Math.Abs(Numerator));

    public readonly bool IsInteger => Denominator == 1;

    public readonly bool IsZero => IsFinite && Numerator == 0;

    public readonly bool IsNonZero => IsFinite && Numerator != 0;

    public readonly bool IsOne => IsFinite && Numerator == 1;

    public readonly bool IsInvertible => IsNonZero;

    public readonly bool IsNaN => Denominator == 0 && Numerator == 0;

    public readonly bool IsNegative => Numerator < 0;

    public readonly bool IsPositive => Numerator > 0;

    public readonly bool IsNegativeInfinity => IsInfinity && IsNegative;

    public readonly bool IsPositiveInfinity => IsInfinity && IsPositive;

    public readonly bool IsInfinity => Denominator == 0 && Numerator != 0;

    public readonly bool IsFinite => Denominator != 0;

    public readonly bool IsBinary => Is(Zero) || Is(One);


    public Fraction(Fraction f)
        : this(f.Numerator, f.Denominator)
    {
    }

    public unsafe Fraction(Fraction* ptr)
        : this(*ptr)
    {
    }

    public Fraction(long numerator, ulong denominator)
    {
        ulong gcd = MathExtensions.GreatestCommonDivisor((ulong)Math.Abs(numerator), denominator);

        if (gcd == 0)
            (numerator, denominator, gcd) = (0, 0, 1);

        Numerator = numerator / (long)gcd;
        Denominator = denominator / gcd;
    }

    public readonly Fraction Negate() => new(-Numerator, Denominator);

    public readonly Fraction Abs() => IsNegative ? Negate() : this;

    public readonly Fraction Add(in Fraction second)
    {
        ulong lcm = MathExtensions.LeastCommonMultiple(Denominator, second.Denominator);
        bint a = Numerator * (bint)(Denominator / lcm);
        bint b = second.Numerator * (bint)(second.Denominator / lcm);

        return new Fraction((long)(a + b), lcm);
    }

    public readonly Fraction Add(params Fraction[] others) => others.Aggregate(this, (x, y) => x.Add(y));

    public readonly Fraction Subtract(in Fraction second) => Add(second.Negate());

    public readonly Fraction Subtract(params Fraction[] others) => others.Aggregate(this, (x, y) => x.Subtract(y));

    public readonly Fraction Increment() => Add(One);

    public readonly Fraction Decrement() => Subtract(One);

    public readonly Fraction Multiply(in Fraction second)
    {
        bint n = (bint)Numerator * second.Numerator;
        bint d = (bint)Denominator * second.Denominator;

        if (d < 0)
        {
            d = -d;
            n = -n;
        }

        bint gcd = MathExtensions.GreatestCommonDivisor(n, d);

        return new Fraction((long)(n / gcd), (ulong)(d / gcd));
    }

    public readonly Fraction Multiply(params Fraction[] others) => others.Aggregate(this, (x, y) => x.Multiply(y));

    public readonly Fraction Divide(in Fraction second)
    {
        bint n = (bint)Numerator * second.Denominator;
        bint d = (bint)Denominator * second.Numerator;

        if (d < 0)
        {
            d = -d;
            n = -n;
        }

        bint gcd = MathExtensions.GreatestCommonDivisor(n, d);

        return new Fraction((long)(n / gcd), (ulong)(d / gcd));
    }

    public readonly Fraction Power(int e) => e switch
    {
        0 => One,
        1 => this,
        < 0 => Power(e).MultiplicativeInverse,
        _ => Multiply(Enumerable.Repeat(this, e - 1).ToArray()),
    };
    /*
        Fraction r = One;
        Fraction p = this;
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
    */

    public readonly Scalar Sqrt() => Scalar.Sqrt(Numerator) / Scalar.Sqrt(Denominator);

    public readonly Scalar Log() => Scalar.Log(Numerator) - Scalar.Log(Denominator);

    public readonly Scalar Log(Scalar basis) => Scalar.Log(this, basis);

    public readonly Fraction Clamp() => Clamp(Zero, One);

    public readonly Fraction Clamp(Fraction low, Fraction high) => Max(low).Min(high);

    public readonly Fraction Max(Fraction second) => CompareTo(second) < 0 ? second : this;

    public readonly Fraction Min(Fraction second) => CompareTo(second) > 0 ? second : this;

    public readonly int CompareTo(Fraction other)
    {
        if (IsNaN)
            return other.IsNaN ? 0 : -1;
        else if (IsNegativeInfinity)
            return other.IsNegativeInfinity ? 0 : -1;
        else if (IsPositiveInfinity)
            return other.IsPositiveInfinity ? 0 : 1;
        else
        {
            ulong lcm = MathExtensions.LeastCommonMultiple(Denominator, other.Denominator);
            bint a = Numerator * (bint)(Denominator / lcm);
            bint b = other.Numerator * (bint)(other.Denominator / lcm);

            return a.CompareTo(b);
        }
    }

    public readonly override bool Equals(object? other) => other is Fraction f && Equals(f);

    public readonly bool Equals(Fraction other) => CompareTo(other) == 0;

    public readonly bool Is([MaybeNull] Fraction o) => Equals(o);

    public readonly bool IsNot([MaybeNull] Fraction o) => !Equals(o);

    public readonly override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public readonly override string ToString() => IsNaN ? "NaN" : IsNegativeInfinity ? "-∞" : IsPositiveInfinity ? "∞" : IsInteger ? Numerator.ToString() : $"{Numerator}/{Denominator}";

    public readonly string ToPrettyString()
    {
        if (IsNaN)
            return "NaN";
        else if (IsNegative)
            return '-' + Negate().ToPrettyString();
        else if (IsInfinity)
            return "∞";
        else if (IsInteger)
            return Numerator.ToString();
        else
        {
            long d = (long)Denominator;
            long n = Numerator / d;
            long r = Numerator % d;
            string str = r.ToSuperScript() + '/' + d.ToSubScript();

            return n > 0 ? n + str : str;
        }
    }


    public static Fraction FromScalar(Scalar value) => FromScalar(value, Scalar.ComputationalEpsilon);

    public static Fraction FromScalar(Scalar value, Scalar accuracy)
    {
        if (value.IsNaN)
            return NaN;
        else if (value.IsPositiveInfinity)
            return PositiveInfinity;
        else if (value.IsNegativeInfinity)
            return NegativeInfinity;

        if (accuracy <= 0 || accuracy >= 1)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "The accuracy must be in the exclusive interval of (0..1).");

        int sign = value.Sign;

        if (sign < 0)
            value = value.AbsoluteValue;

        double maxError = sign == 0 ? accuracy : value * accuracy;
        long n = (long)value;

        value -= n;

        if (value < maxError)
            return new Fraction(sign * n, 1);
        else if (1 - maxError < value)
            return new Fraction(sign * (n + 1), 1);

        Scalar z = value;
        ulong previousDenominator = 0;
        ulong denominator = 1;
        long numerator;

        do
        {
            z = 1.0 / (z - (long)z);

            ulong temp = denominator;

            denominator = (denominator * (ulong)z) + previousDenominator;
            previousDenominator = temp;
            numerator = (long)(value * denominator);
        }
        while (!z.IsInteger && (value - (Scalar)numerator / denominator).Abs() > maxError);

        return new Fraction((long)(((Scalar)n * denominator + numerator) * sign), denominator);
    }


    public static implicit operator Fraction((long  numerator, ulong denominator) fraction) => new(fraction.numerator, fraction.denominator);

    public static implicit operator Scalar(Fraction f) => (Scalar)f.Numerator / (Scalar)f.Denominator;

    public static implicit operator float(Fraction f) => (float)(Scalar)f;

    public static implicit operator double(Fraction f) => (double)(Scalar)f;

    public static implicit operator decimal(Fraction f) => (decimal)(Scalar)f;

    public static explicit operator sbyte(Fraction f) => (sbyte)(Scalar)f;

    public static explicit operator byte(Fraction f) => (byte)(Scalar)f;

    public static explicit operator short(Fraction f) => (short)(Scalar)f;

    public static explicit operator ushort(Fraction f) => (ushort)(Scalar)f;

    public static explicit operator int(Fraction f) => (int)(Scalar)f;

    public static explicit operator uint(Fraction f) => (uint)(Scalar)f;

    public static explicit operator long(Fraction f) => (long)(Scalar)f;

    public static explicit operator ulong(Fraction f) => (ulong)(Scalar)f;

    public static implicit operator Fraction(sbyte n) => new(n, 1);

    public static implicit operator Fraction(byte n) => new(n, 1);

    public static implicit operator Fraction(short n) => new(n, 1);

    public static implicit operator Fraction(ushort n) => new(n, 1);

    public static implicit operator Fraction(int n) => new(n, 1);

    public static implicit operator Fraction(uint n) => new(n, 1);

    public static implicit operator Fraction(long n) => new(n, 1);

    public static explicit operator Fraction(ulong n) => new((long)n, 1);

    public static explicit operator Fraction(Scalar s) => FromScalar(s);

    public static explicit operator Fraction(float s) => (Fraction)(Scalar)s;

    public static explicit operator Fraction(double s) => (Fraction)(Scalar)s;

    public static explicit operator Fraction(decimal s) => (Fraction)(Scalar)s;

    public static Fraction operator +(in Fraction f) => f;

    public static Fraction operator -(in Fraction f) => f.Negate();

    public static Fraction operator ++(in Fraction f) => f.Increment();

    public static Fraction operator --(in Fraction f) => f.Decrement();

    public static Fraction operator +(in Fraction f1, in Fraction f2) => f1.Add(in f2);

    public static Fraction operator -(in Fraction f1, in Fraction f2) => f1.Subtract(in f2);

    public static Fraction operator *(in Fraction f1, in Fraction f2) => f1.Multiply(in f2);

    public static Fraction operator /(in Fraction f1, in Fraction f2) => f1.Divide(in f2);

    public static Fraction operator ^(Fraction f, int e) => f.Power(e);

    // public static Fraction operator %(Fraction f1, Fraction f2) => f1.Modulus(in f2);

    public static bool operator ==(Fraction f1, Fraction f2) => f1.Is(f2);

    public static bool operator !=(Fraction f1, Fraction f2) => f1.IsNot(f2);

    public static bool operator <=(Fraction f1, Fraction f2) => f1.CompareTo(f2) <= 0;

    public static bool operator >=(Fraction f1, Fraction f2) => f1.CompareTo(f2) >= 0;

    public static bool operator <(Fraction f1, Fraction f2) => f1.CompareTo(f2) < 0;

    public static bool operator >(Fraction f1, Fraction f2) => f1.CompareTo(f2) > 0;
}
