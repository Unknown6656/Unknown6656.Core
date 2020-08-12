using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

using Unknown6656.Mathematics.Numerics;
using Unknown6656.Common;

namespace Unknown6656.Mathematics.Cryptography
{
    public unsafe abstract class HashFunction<T>
        where T : HashFunction<T>
    {
        /// <summary>
        /// Returns the hash value's size (in bytes).
        /// </summary>
        public abstract int HashSize { get; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract byte[] Hash(byte[] data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash(byte[] data, int offset, int length) => Hash(data[offset..(offset + length)]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash(Stream stream)
        {
            using MemoryStream ms = new MemoryStream();

            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Hash(ms);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash(MemoryStream stream) => Hash(stream.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash(string data) => Hash(data, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash(string data, Encoding encoding) => Hash(encoding.GetBytes(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash<X>(X data) where X : unmanaged => Hash(&data, sizeof(X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual byte[] Hash<X>(X* pointer, int size)
            where X : unmanaged
        {
            byte[] data = new byte[size];
            byte* src = (byte*)pointer;

            for (int i = 0; i < size; ++i)
                data[i] = src[i];

            return Hash(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OAEP<T, Random> CreateOAEP<Random>(Random random) where Random : Numerics.Random => new OAEP<T, Random>((T)this, random);
    }

//  public abstract class HashFunction<T, Algorithm>
//      : HashFunction<T>
//      , IDisposable
//      where Hash : HashFunction<T, Algorithm>
//      where Algorithm : HashAlgorithm
//  {
//      public Algorithm InternalHashAlgorithm { get; }
//
//      public override int HashSize => InternalHashAlgorithm.;
//
//
//      public HashFunction()
//          : this()
//      {
//      }
//
//      public HashFunction(Algorithm algorithm) => InternalHashAlgorithm = algorithm;
//
//      public override byte[] Hash(byte[] data) => throw new NotImplementedException();
//  }

    public static partial class HashFunctions
    {
        // TODO : all hashfunctions
    }

    public sealed unsafe class OAEP<Hash, Random>
        where Hash : HashFunction<Hash>
        where Random : Numerics.Random
    {
        public Hash HashFunction { get; }
        public Random RandomGenerator { get; }
        public int Rounds { set; get; } = 1;


        public OAEP(Hash hash, Random random)
        {
            HashFunction = hash;
            RandomGenerator = random;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(byte[] data)
        {
            int hashsz = HashFunction.HashSize;
            int length = data.Length;
            int sz_m = (int)Math.Ceiling((length + 4d) / hashsz) * hashsz;
            byte[] result = new byte[sz_m + hashsz];

            lock (RandomGenerator)
                RandomGenerator.Fill(result, sz_m, hashsz);

            fixed (byte* ptr = result)
                *(int*)ptr = length;

            for (int i = 0; i < length; ++i)
                result[i + 4] = data[i];

            for (int round = 0; round < Rounds; ++round)
            {
                byte[] G = HashFunction.Hash(result[(sz_m + 1)..]);

                for (int i = 0; i < sz_m; ++i)
                    result[i] ^= G[i % hashsz];

                byte[] H = HashFunction.Hash(result[0..sz_m]);

                for (int i = 0; i < hashsz; ++i)
                    result[i + sz_m] ^= H[i];
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(byte[] data, int offset, int length) => Pad(data[offset..(offset + length)]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(Stream stream)
        {
            using MemoryStream ms = new MemoryStream();

            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Pad(ms);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(MemoryStream stream) => Pad(stream.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(string data) => Pad(data, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad(string data, Encoding encoding) => Pad(encoding.GetBytes(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad<T>(T data) where T : unmanaged => Pad(&data, sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Pad<T>(T* pointer, int size)
            where T : unmanaged
        {
            byte[] data = new byte[size];
            byte* src = (byte*)pointer;

            for (int i = 0; i < size; ++i)
                data[i] = src[i];

            return Pad(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] UnpadBytes(byte[] data)
        {
            int hashsz = HashFunction.HashSize;
            int sz_m = data.Length - hashsz;
            int datasz = sz_m;

            for (int round = 0; round < Rounds; ++round)
            {
                byte[] H = HashFunction.Hash(data[0..sz_m]);

                for (int i = 0; i < hashsz; ++i)
                    data[i + sz_m] ^= H[i];

                byte[] G = HashFunction.Hash(data[(sz_m + 1)..]);

                for (int i = 0; i < sz_m; ++i)
                    data[i] ^= G[i % hashsz];
            }

            fixed (byte* ptr = data)
                datasz = *(int*)ptr;

            return data[4..(4 + datasz)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string UnpadString(byte[] data) => UnpadString(data, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string UnpadString(byte[] data, Encoding encoding) => encoding.GetString(UnpadBytes(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T UnpadData<T>(byte[] data)
            where T : unmanaged
        {
            data = UnpadBytes(data);

            fixed (byte* ptr = data)
                return *(T*)ptr;
        }
    }
}
