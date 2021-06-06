using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Common;
using Unknown6656.IO;

namespace Unknown6656.Mathematics.Numerics
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public unsafe sealed class VarInt
         : IComparable<VarInt>
         , IEquatable<VarInt>
         , IComparable
         , IConvertible
         , ICloneable
    {
        internal byte[] InternalBytes { get; }

        public int Size => InternalBytes.Length;


        private VarInt(byte[] raw) => InternalBytes = raw;

        public override string ToString() => ToBigInteger().ToString();

        private string GetDebuggerDisplay() => $"{this}     ({Size} Byte(s): {DataStream.FromBytes(InternalBytes).ToHexString(false, true)})";

        public override int GetHashCode() => DataStream.FromArray(InternalBytes).Hash(HashFunctions.CRC32).ToUnmanaged<int>();

        public override bool Equals(object? obj) => obj is VarInt other && Equals(other);

        public bool Equals(VarInt? other) => GetHashCode() == other?.GetHashCode();

        public int CompareTo(VarInt? other) => other is null ? -1 : ToBigInteger().CompareTo(other.ToBigInteger());

        public int CompareTo(object? obj) => CompareTo(obj as VarInt);

        private T To<T>() where T : unmanaged => InternalBytes.Take(sizeof(T)).ToArray().BinaryCast<T>();

        public sbyte ToSByte(IFormatProvider? provider) => unchecked((sbyte)ToByte(provider));

        public byte ToByte(IFormatProvider? provider) => InternalBytes.Length == 0 ? (byte)0 : InternalBytes[0];

        public short ToInt16(IFormatProvider? provider) => To<short>();

        public ushort ToUInt16(IFormatProvider? provider) => To<ushort>();

        public int ToInt32(IFormatProvider? provider) => To<int>();

        public uint ToUInt32(IFormatProvider? provider) => To<uint>();

        public long ToInt64(IFormatProvider? provider) => To<long>();

        public ulong ToUInt64(IFormatProvider? provider) => To<ulong>();

        public float ToSingle(IFormatProvider? provider) => (float)ToDecimal(provider);

        public double ToDouble(IFormatProvider? provider) => (double)ToDecimal(provider);

        public decimal ToDecimal(IFormatProvider? provider) => (decimal)ToBigInteger();

        public byte ToByte() => ToByte(null);

        public decimal ToDecimal() => ToDecimal(null);

        public double ToDouble() => ToDouble(null);

        public short ToInt16() => ToInt16(null);

        public int ToInt32() => ToInt32(null);

        public long ToInt64() => ToInt64(null);

        public sbyte ToSByte() => ToSByte(null);

        public float ToSingle() => ToSingle(null);

        public ushort ToUInt16() => ToUInt16(null);

        public uint ToUInt32() => ToUInt32(null);

        public ulong ToUInt64() => ToUInt64(null);

        public UInt128 ToUInt128() => To<UInt128>();

        public Scalar ToScalar() => (Scalar)ToDecimal();

        public BigInteger ToBigInteger() => new BigInteger(InternalBytes);

        public VarInt Clone() => new VarInt(InternalBytes.ToArray());

        object ICloneable.Clone() => Clone();

        TypeCode IConvertible.GetTypeCode() => TypeCode.Object;

        bool IConvertible.ToBoolean(IFormatProvider? provider) => ToByte(provider) != 0;

        char IConvertible.ToChar(IFormatProvider? provider) => (char)ToUInt16(provider);

        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => new DateTime(ToInt64(provider));

        string IConvertible.ToString(IFormatProvider? provider) => ToString();

        object IConvertible.ToType(Type type, IFormatProvider? provider) => Convert.ChangeType(this, type, provider);


        public byte[] Serialize()
        {
            if (InternalBytes.Length == 1 && InternalBytes[0] < 0x80)
                return InternalBytes;

            byte[] output = new byte[(int)Math.Ceiling(InternalBytes.Length * 9d / 8d)];
            void set_bit(int index, bool bit)
            {
                ref byte b = ref output[index / 8];
                int mask = 1 << 7 - index % 8;

                if (bit)
                    b |= (byte)mask;
                else
                    b &= (byte)~mask;
            }

            for (int i = 0; i < InternalBytes.Length; ++i)
            {
                bool has_next = i == 0 || i * 9 % 8 != 0;

                set_bit(i * 9, has_next);

                for (int j = 0; j < 8; ++j)
                    set_bit(i * 9 + j + 1, ((InternalBytes[i] >> 7 - j) & 1) != 0);
            }

            return output;
        }

        public static VarInt Deserialize(byte[] serialized)
        {
            using MemoryStream ms = new MemoryStream(serialized);

            return Deserialize(ms);
        }

        public static VarInt Deserialize(Stream stream)
        {
            List<byte> bytes = new();
            bool has_next = true;
            int bit_index = 0;
            byte current = 0;
            bool first = true;
            int input;

            do
            {
                input = stream.ReadByte();

                if (input < 0)
                {
                    if (!first)
                        bytes.Add(current);

                    has_next = false;
                }
                else if (first && input < 0x80)
                {
                    bytes.Add((byte)input);
                    has_next = false;
                }
                else
                    for (int i = 0; i < 8; ++i)
                    {
                        bool bit = ((input >> (7 - i)) & 1) != 0;

                        if (bit_index % 9 == 0)
                        {
                            if (!first)
                                bytes.Add(current);

                            if (!has_next)
                                break;

                            has_next = bit;
                            current = 0;
                        }
                        else if (bit)
                            current |= (byte)(1 << (8 - bit_index % 9));

                        ++bit_index;
                    }

                first = false;
            }
            while (has_next);

            return new VarInt(bytes.ToArray());
        }

        public static VarInt FromBytes(byte[] bytes) => new VarInt(bytes);

        public static VarInt FromNumber(byte value) => FromBytes(new[] { value });

        public static VarInt FromNumber(sbyte value) => FromBytes(new[] { unchecked((byte)value) });

        public static VarInt FromNumber(short value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(ushort value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(int value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(uint value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(long value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(ulong value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(UInt128 value) => FromNumber((BigInteger)value);

        public static VarInt FromNumber(BigInteger value) => FromBytes(value.ToByteArray());

        public static VarInt FromNumber(float value) => FromNumber((decimal)value);

        public static VarInt FromNumber(double value) => FromNumber((decimal)value);

        public static VarInt FromNumber(Scalar value) => FromNumber((decimal)value);

        public static VarInt FromNumber(decimal value) => FromNumber((BigInteger)value);


        public static bool operator ==(VarInt v1, VarInt v2) => v1.Equals(v2);

        public static bool operator !=(VarInt v1, VarInt v2) => !(v1 == v2);

        public static bool operator <(VarInt v1, VarInt v2) => v1.CompareTo(v2) < 0;

        public static bool operator <=(VarInt v1, VarInt v2) => v1.CompareTo(v2) <= 0;

        public static bool operator >=(VarInt v1, VarInt v2) => v1.CompareTo(v2) >= 0;

        public static bool operator >(VarInt v1, VarInt v2) => v1.CompareTo(v2) > 0;

        public static implicit operator VarInt(byte value) => FromNumber(value);

        public static implicit operator VarInt(sbyte value) => FromNumber(value);

        public static implicit operator VarInt(short value) => FromNumber(value);

        public static implicit operator VarInt(ushort value) => FromNumber(value);

        public static implicit operator VarInt(int value) => FromNumber(value);

        public static implicit operator VarInt(uint value) => FromNumber(value);

        public static implicit operator VarInt(long value) => FromNumber(value);

        public static implicit operator VarInt(ulong value) => FromNumber(value);

        public static implicit operator VarInt(UInt128 value) => FromNumber(value);

        public static implicit operator VarInt(BigInteger value) => FromNumber(value);

        public static explicit operator VarInt(float value) => FromNumber(value);

        public static explicit operator VarInt(double value) => FromNumber(value);

        public static explicit operator VarInt(Scalar value) => FromNumber(value);

        public static explicit operator VarInt(decimal value) => FromNumber(value);

        public static explicit operator byte(VarInt value) => value.ToByte();

        public static explicit operator sbyte(VarInt value) => value.ToSByte();

        public static explicit operator short(VarInt value) => value.ToInt16();

        public static explicit operator ushort(VarInt value) => value.ToUInt16();

        public static explicit operator int(VarInt value) => value.ToInt32();

        public static explicit operator uint(VarInt value) => value.ToUInt32();

        public static explicit operator long(VarInt value) => value.ToInt64();

        public static explicit operator ulong(VarInt value) => value.ToUInt64();

        public static explicit operator UInt128(VarInt value) => value.ToUInt128();

        public static explicit operator BigInteger(VarInt value) => value.ToBigInteger();

        public static explicit operator float(VarInt value) => value.ToSingle();

        public static explicit operator double(VarInt value) => value.ToDouble();

        public static explicit operator Scalar(VarInt value) => value.ToScalar();

        public static explicit operator decimal(VarInt value) => value.ToDecimal();
    }
}
