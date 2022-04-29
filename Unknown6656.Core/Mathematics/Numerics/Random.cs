using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Generics;
using Unknown6656.Common;
using Unknown6656.IO;

using netrandom = System.Random;

namespace Unknown6656.Mathematics.Numerics;


public abstract unsafe class Random
{
    public static BuiltinRandom BuiltinRandom { get; } = new BuiltinRandom();
    public static XorShift XorShift { get; } = new XorShift();

    public long Seed { get; }


    public Random()
        : this(Guid.NewGuid().BinaryCast<Guid, long>() ^ DateTime.UtcNow.Ticks)
    {
    }

    public Random(long seed)
    {
        Seed = seed;
        Init();
    }

    protected abstract void Init();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte NextByte() => (byte)(NextInt() & 0xff);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte NextSByte() => (sbyte)NextByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char NextChar() => (char)NextShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short NextShort() => (short)NextUShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort NextUShort() => (ushort)(NextInt() & 0xffff);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat() => (float)NextDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble() => ((double)NextULong() / long.MaxValue) % 1d;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal NextDecimal() => (decimal)NextDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar NextScalar() => NextDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextBool() => NextBool(.5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NextBool(Scalar true_probability) => NextDouble() >= true_probability;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte[] NextSBytes(int count) => Enumerable.Range(0, count).ToArray(_ => NextSByte());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char[] NextChars(int count) => Enumerable.Range(0, count).ToArray(_ => NextChar());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short[] NextShorts(int count) => Enumerable.Range(0, count).ToArray(_ => NextShort());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort[] NextUShorts(int count) => Enumerable.Range(0, count).ToArray(_ => NextUShort());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float[] NextFloats(int count) => Enumerable.Range(0, count).ToArray(_ => NextFloat());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] NextDoubles(int count) => Enumerable.Range(0, count).ToArray(_ => NextDouble());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal[] NextDecimals(int count) => Enumerable.Range(0, count).ToArray(_ => NextDecimal());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] NextScalars(int count) => Enumerable.Range(0, count).ToArray(_ => NextScalar());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool[] NextBools(int count) => Enumerable.Range(0, count).ToArray(_ => NextBool());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool[] NextBools(int count, Scalar true_probability) => Enumerable.Range(0, count).ToArray(_ => NextBool(true_probability));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Choose<T>(ref T left, ref T right) => ref Choose(ref left, ref right, .5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Choose<T>(ref T left, ref T right, Scalar right_probability) => ref NextBool(right_probability) ? ref right : ref left;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextGaussian(float mean, float deviation) => (float)NextGaussian((double)mean, deviation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal NextGaussian(decimal mean, decimal deviation) => (decimal)NextGaussian((double)mean, (double)deviation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar NextGaussian(Scalar mean, Scalar deviation) => NextGaussian(mean.Determinant, deviation.Determinant);

    public double NextGaussian(double mean, double deviation)
    {
        double u1 = 1 - NextDouble();
        double u2 = 1 - NextDouble();

        return mean + deviation * Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(Math.Tau * u2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float[] NextGaussians(int count, float mean, float deviation) => Enumerable.Range(0, count).ToArray(_ => NextGaussian(mean, deviation));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal[] NextGaussians(int count, decimal mean, decimal deviation) => Enumerable.Range(0, count).ToArray(_ => NextGaussian(mean, deviation));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] NextGaussians(int count, Scalar mean, Scalar deviation) => Enumerable.Range(0, count).ToArray(_ => NextGaussian(mean, deviation));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] NextGaussians(int count, double mean, double deviation) => Enumerable.Range(0, count).ToArray(_ => NextGaussian(mean, deviation));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] NextScalars(int count, Scalar max) => Enumerable.Range(0, count).ToArray(_ => NextScalar(max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] NextScalars(int count, Scalar min, Scalar max) => Enumerable.Range(0, count).ToArray(_ => NextScalar(min, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex[] NextComplexes(int count, Scalar length) => Enumerable.Range(0, count).ToArray(_ => NextComplex(length));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex[] NextComplexes(int count) => Enumerable.Range(0, count).ToArray(_ => NextComplex());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] NextInts(int count) => Enumerable.Range(0, count).ToArray(_ => NextInt());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint[] NextUInts(int count) => Enumerable.Range(0, count).ToArray(_ => NextUInt());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long[] NextLongs(int count) => Enumerable.Range(0, count).ToArray(_ => NextLong());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong[] NextULongs(int count) => Enumerable.Range(0, count).ToArray(_ => NextULong());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128[] NextUInt128s(int count) => Enumerable.Range(0, count).ToArray(_ => NextUInt128());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar NextScalar(Scalar max) => NextScalar() * max;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar NextScalar(Scalar min, Scalar max) => min + NextScalar(max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex NextComplex() => new(NextScalar(), NextScalar());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Complex NextComplex(Scalar length) => NextComplex().Normalized * length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract int NextInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextInt(int max) => (int)(NextUInt() / ((float)uint.MaxValue + 1) * max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextInt(int min, int max) => min + NextInt(max - min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint NextUInt() => (uint)NextInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long NextLong() => Next<long>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextULong() => Next<ulong>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128 NextUInt128() => Next<UInt128>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] NextBytes(int count) => Fill(new byte[count]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Fill(byte[] buffer) => Fill(buffer, 0, buffer.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Fill(byte[] buffer, int offset) => Fill(buffer, offset, buffer.Length - offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual byte[] Fill(byte[] buffer, int offset, int length)
    {
        fixed (byte* bptr = buffer ??= new byte[length + offset])
        {
            int* iptr = (int*)(bptr + offset);
            int i = 0;

            while (i < length)
                if (length - i > 4)
                {
                    iptr[i / 4] = NextInt();
                    i += 4;
                }
                else
                    bptr[i++ + offset] = NextByte();
        }

        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> Shuffle<T>(IEnumerable<T> collection) => collection.Shuffle(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Choose<T>(params T[] items) => ref items[NextInt(items.Length)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Next<T>()
        where T : unmanaged
    {
        T res = default;

        if (sizeof(T) % 4 == 0)
        {
            int* ptr = (int*)&res;

            for (int i = 0, s = sizeof(T) / 4; i < s; ++i)
                ptr[i] = NextInt();
        }
        else
        {
            byte* ptr = (byte*)&res;

            for (int i = 0, s = sizeof(T); i < s; ++i)
                ptr[i] = NextByte();
        }

        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] Next<T>(int count) where T : unmanaged => Enumerable.Range(0, count).ToArray(_ => Next<T>());

    public static implicit operator Random(netrandom random) => new BuiltinRandom._(random);

    public static implicit operator Stream(Random random) => ByteGeneratorStream.FromRandom(random);

}

public sealed class XorShift
    : Random
{
    private volatile uint _x, _y, _z, _w;


    public XorShift()
        : base()
    {
    }

    public XorShift(long seed)
        : base(seed)
    {
    }

    protected override void Init()
    {
        _x = (uint)(Seed & 0xffffffffu);
        _y = (uint)(Seed >> 32);
        _z = _x.ROL((int)_y);
        _w = _x ^ (_z ^ _y).ROR((int)_x);

        if (_x == 0)
            _x = 0x6a598431;
        if (_y == 0)
            _y = 0x94b86cf9;
        if (_z == 0)
            _z = (uint)(_x ^ Seed ^ _y);
        if (_w == 0)
            _w = _x.ROL((int)_y) ^ _y.ROL((int)_x) + 1;
    }

    public override int NextInt()
    {
        uint s = _x;
        uint t = _w;

        _w = _z;
        _z = _y;
        _y = s;

        t ^= t << 11;
        t ^= t >> 8;
        _x = t ^ s ^ (s >> 19);

        return (int)_x;
    }
}

public sealed class CongruenceGenerator
    : Random
{
    private ModuloRing _x;


    protected override void Init() => (_x, _) = TextbookRSA.GenerateKeyPair();

    public override int NextInt() => (_x = _x.Power(2)).GetHashCode();
}

public class BuiltinRandom
    : Random
{
    private netrandom _random;


    public BuiltinRandom()
        : base() => _random = new netrandom((int)Seed);

    public BuiltinRandom(int seed)
        : base(seed) => _random = new netrandom((int)Seed);

    protected override void Init() => _random = new netrandom((int)Seed);

    public override int NextInt() => _random.Next();

    public static implicit operator netrandom(BuiltinRandom random) => random._random;


    internal sealed class _
        : BuiltinRandom
    {
        public _(netrandom r)
            : base() => _random = r;

        protected override void Init()
        {
        }
    }
}

public sealed class CryptoRandom<RNG>
    : Random
    where RNG : RandomNumberGenerator
{
    public RNG RandomNumberGenerator { get; }


    public CryptoRandom(RNG rng) => RandomNumberGenerator = rng;

    public override int NextInt()
    {
        byte[] bytes = new byte[sizeof(int)];

        RandomNumberGenerator.GetBytes(bytes);

        return bytes.BinaryCast<int>();
    }

    protected override void Init()
    {
    }
}

public sealed class TurbulenceShift
    : Random
{
    private volatile int _state;


    public TurbulenceShift()
        : base()
    {
    }

    public TurbulenceShift(long seed)
        : base(seed)
    {
    }

    protected override void Init() => _state = Seed.GetHashCode();

    public override int NextInt()
    {
        uint i = (uint)_state;

        i ^= 2747636419u;
        i *= 2654435769u;
        i ^= i >> 16;
        i *= 2654435769u;
        i ^= i >> 16;
        i *= 2654435769u;

        return _state = (int)i;
    }
}


// TODO : mersenne twister https://de.wikipedia.org/wiki/Mersenne-Twister

public sealed class MersenneTwisterMT19937
{
}

public sealed class MersenneTwisterTT800
{
}
