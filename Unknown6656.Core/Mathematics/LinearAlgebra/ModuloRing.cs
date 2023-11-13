using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Analysis;

namespace Unknown6656.Mathematics.LinearAlgebra;


[StructLayout(LayoutKind.Sequential), NativeCppClass, Serializable, CLSCompliant(false)]
public unsafe readonly /* ref */ partial struct ModuloRing
    : IField<ModuloRing>
    , INumericScalar<ModuloRing>
    , IComparable
    , ICloneable
{
    private readonly bint _val;
    private readonly bint _mod;


    public static ModuloRing One { get; } = 1;

    public static ModuloRing Zero { get; } = 0;


    public readonly bint Value => _val;

    public readonly bint Modulus => _mod;

    public readonly ModuloRing MultiplicativeInverse => Invert();

    public readonly bool IsInvertible => IsNonZero && MathExtensions.GreatestCommonDivisor(Value, Modulus) == 1;

    public readonly bool IsOne => Value == 1;

    public readonly ModuloRing AdditiveInverse => Negate();

    public readonly bool IsZero => Value == 0;

    public readonly bool IsNonZero => Value != 0;

    public readonly bool IsPrime => Value.IsPrime();

    public readonly ModuloRing[] PrimeFactors => Do((v, m) => v.PrimeFactorization().Select(f => new ModuloRing(f, m)).Distinct().ToArray());

    public readonly ModuloRing Phi
    {
        get
        {
            bint? φ = null;
            bint n = Value;

            while (φ is null)
            {
                φ = n.Phi();
                n += Modulus;
            }

            return new ModuloRing(φ.Value, Modulus);
        }
    }

    public readonly IEnumerable<bint> CongruenceClass => Do((v, m) => Sequences.AllIntegers.Select(i => v + i * m));


    public ModuloRing(bint value, bint modulus)
    {
        if (modulus < 1)
            throw new ArgumentOutOfRangeException(nameof(modulus), "The modulus must be a positive integer larger than zero.");

        _val = ((value % modulus) + modulus) % modulus;
        _mod = modulus;
    }

    private readonly T Do<T>(Func<bint, bint, T> f) => f(Value, Modulus);

    private readonly void Do(Action<bint, bint> f) => f(Value, Modulus);

    public readonly ModuloRing Invert()
    {
        Stack<bint> q = new();
        bint a = Value, b = Modulus, r;

        do
        {
            q.Push(a / b);
            r = a % b;
            a = b;
            b = r;
        }
        while (r != 0);

        bint x = 0;
        bint y = 1;

        q.Pop();

        do
        {
            bint tmp_x = y;

            y = x - y * q.Pop();
            x = tmp_x;
        }
        while (q.Count > 0);

        return new ModuloRing(x, Modulus);
    }

    public readonly ModuloRing Negate() => new(Modulus - Value, Modulus);

    public readonly ModuloRing Add(bint value) => new(Value + value, Modulus);

    public readonly ModuloRing Add(in ModuloRing second) => new(Value + second.Value, bint.Min(Modulus, second.Modulus));

    public readonly ModuloRing Add(params ModuloRing[] others) => others.Aggregate(this, (x, y) => x.Add(y));

    public readonly ModuloRing Subtract(bint value) => new(Value - value, Modulus);

    public readonly ModuloRing Subtract(in ModuloRing second) => Add(second.Negate());

    public readonly ModuloRing Subtract(params ModuloRing[] others) => others.Aggregate(this, (x, y) => x.Subtract(y));

    public readonly ModuloRing Increment() => Add(1);

    public readonly ModuloRing Decrement() => Subtract(1);

    public readonly ModuloRing Multiply(bint value) => new(Value * value, Modulus);

    public readonly ModuloRing Multiply(in ModuloRing second) => new(Value * second.Value, bint.Min(Modulus, second.Modulus));

    public readonly ModuloRing Multiply(params ModuloRing[] others) => others.Aggregate(this, (x, y) => x.Multiply(y));

    public readonly ModuloRing Divide(bint value) => new(Value / value, Modulus);

    public readonly ModuloRing Divide(in ModuloRing second) => Multiply(second.MultiplicativeInverse);

    public readonly ModuloRing Modulo(bint value) => new(Value % value, Modulus);

    public readonly ModuloRing Modulo(in ModuloRing second) => new(Value % second.Value, Modulus);

    readonly ModuloRing IField<ModuloRing>.Modulus(in ModuloRing second) => Modulo(in second);

    public readonly ModuloRing Power(int e) => new(bint.ModPow(Value, e, Modulus), Modulus);

    public readonly (ModuloRing P, ModuloRing Q)? DecomposePQ(ModuloRing phi)
    {
        if (new Polynomial((double)Value, (double)(phi.Value - Value - 1), 1).Roots.ToArray() is { Length: 2 } arr)
            return ((arr[0], Modulus), (arr[1], Modulus));

        return null;
    }

    public readonly ModuloRing Abs() => this;

    public readonly ModuloRing Min(ModuloRing second) => CompareTo(second) <= 0 ? this : second;

    public readonly ModuloRing Max(ModuloRing second) => CompareTo(second) >= 0 ? this : second;

    public readonly ModuloRing Clamp() => new(IsZero ? 0 : 1, Modulus);

    public readonly ModuloRing Clamp(ModuloRing low, ModuloRing high) => Max(low).Min(high);

    public readonly bool Is(ModuloRing o) => Value.Equals(Value) && Modulus.Equals(Modulus);

    public readonly bool IsNot(ModuloRing o) => !Is(o);

    public readonly bool Equals(ModuloRing other) => Is(other);

    public readonly override bool Equals(object obj) => obj is ModuloRing m ? Is(m) : false;

    public readonly override int GetHashCode() => HashCode.Combine(Value, Modulus);

    public readonly object Clone() => this;

    public readonly int CompareTo(ModuloRing other) => Value.CompareTo(other.Value);

    public readonly int CompareTo(object other) => other is ModuloRing r ? CompareTo(r) : throw new ArgumentException($"The given object must be of the type '{typeof(ModuloRing)}'.", nameof(other));

    public readonly override string ToString() => $"{Value} (mod {Modulus})";


    public static implicit operator ModuloRing(sbyte b) => new(b, (bint)sbyte.MaxValue + 1);

    public static implicit operator ModuloRing(byte b) => new(b, (bint)byte.MaxValue + 1);

    public static implicit operator ModuloRing(short s) => new(s, (bint)short.MaxValue + 1);

    public static implicit operator ModuloRing(ushort s) => new(s, (bint)ushort.MaxValue + 1);

    public static implicit operator ModuloRing(int i) => new(i, (bint)int.MaxValue + 1);

    public static implicit operator ModuloRing(uint i) => new(i, (bint)uint.MaxValue + 1);

    public static implicit operator ModuloRing(long l) => new(l, (bint)long.MaxValue + 1);

    public static implicit operator ModuloRing(ulong l) => new(l, (bint)ulong.MaxValue + 1);

    public static explicit operator ModuloRing(bint b) => new(b, b + 1);

    public static implicit operator ModuloRing((bint val, bint mod) t) => new(t.val, t.mod);

    public static implicit operator bint(ModuloRing r) => r.Value;

    public static implicit operator sbyte(ModuloRing r) => (sbyte)r.Value;

    public static implicit operator byte(ModuloRing r) => (byte)r.Value;

    public static implicit operator short(ModuloRing r) => (short)r.Value;

    public static implicit operator ushort(ModuloRing r) => (ushort)r.Value;

    public static implicit operator int(ModuloRing r) => (int)r.Value;

    public static implicit operator uint(ModuloRing r) => (uint)r.Value;

    public static implicit operator long(ModuloRing r) => (long)r.Value;

    public static implicit operator ulong(ModuloRing r) => (ulong)r.Value;

    public static bool operator ==(ModuloRing r1, ModuloRing r2) => r1.Is(r2);

    public static bool operator !=(ModuloRing r1, ModuloRing r2) => !(r1 == r2);

    public static ModuloRing operator ~(ModuloRing r) => r.MultiplicativeInverse;

    public static ModuloRing operator +(in ModuloRing r) => r;

    public static ModuloRing operator -(in ModuloRing r) => r.Negate();

    public static ModuloRing operator ++(in ModuloRing r) => r.Increment();

    public static ModuloRing operator --(in ModuloRing r) => r.Decrement();

    public static ModuloRing operator +(bint b, in ModuloRing r) => r.Add(b);

    public static ModuloRing operator +(in ModuloRing r, bint b) => r.Add(b);

    public static ModuloRing operator +(in ModuloRing r1, in ModuloRing r2) => r1.Add(r2);

    public static ModuloRing operator -(bint b, in ModuloRing r) => new ModuloRing(b, r.Modulus).Subtract(r);

    public static ModuloRing operator -(in ModuloRing r, bint b) => r.Subtract(b);

    public static ModuloRing operator -(in ModuloRing r1, in ModuloRing r2) => r1.Subtract(r2);

    public static ModuloRing operator *(bint b, in ModuloRing r) => r.Multiply(b);

    public static ModuloRing operator *(in ModuloRing r, bint b) => r.Multiply(b);

    public static ModuloRing operator *(in ModuloRing r1, in ModuloRing r2) => r1.Multiply(r2);

    public static ModuloRing operator /(bint b, in ModuloRing r) => new ModuloRing(b, r.Modulus).Divide(r);

    public static ModuloRing operator /(in ModuloRing r, bint b) => r.Divide(b);

    public static ModuloRing operator /(in ModuloRing r1, in ModuloRing r2) => r1.Divide(in r2);

    public static ModuloRing operator %(bint b, ModuloRing r) => new ModuloRing(b, r.Modulus).Modulo(r);

    public static ModuloRing operator %(in ModuloRing r, bint b) => r.Modulo(b);

    public static ModuloRing operator %(in ModuloRing r1, in ModuloRing r2) => r1.Modulo(in r2);

    public static ModuloRing operator ^(bint b, ModuloRing r) => new ModuloRing(b, r.Modulus).Power(r);

    public static ModuloRing operator ^(ModuloRing r, bint b) => r.Power((int)b);

    public static ModuloRing operator ^(ModuloRing r1, ModuloRing r2) => r1.Power(r2);

    public static ModuloRing operator <<(ModuloRing r, int i) => new(r.Value << i, r.Modulus);

    public static ModuloRing operator >>(ModuloRing r, int i) => new(r.Value >> i, r.Modulus);
}
