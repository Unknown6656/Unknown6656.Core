using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System;

using Unknown6656.Common;


namespace Unknown6656.Mathematics.Cryptography
{
    public abstract class StringCipher
    {
        public abstract string Encrypt(string key, string message);

        public abstract string Decrypt(string key, string cipher);
    }

    public abstract class BinaryCipher
        : StringCipher
    {
        public static Encoding DefaultEncoding => BytewiseEncoding.Instance;


        public abstract byte[] Encrypt(byte[] key, byte[] message);

        public abstract byte[] Decrypt(byte[] key, byte[] cipher);

        public virtual byte[] Encrypt(byte[] key, Stream message)
        {
            using MemoryStream ms = new();

            message.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Encrypt(key, ms.ToArray());
        }

        public virtual byte[] Decrypt(byte[] key, Stream cipher)
        {
            using MemoryStream ms = new();

            cipher.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Decrypt(key, ms.ToArray());
        }

        public virtual async Task<byte[]> EncryptAsync(byte[] key, Stream message)
        {
            using MemoryStream ms = new();
            await message.CopyToAsync(ms);

            ms.Seek(0, SeekOrigin.Begin);

            return Encrypt(key, ms.ToArray());
        }

        public virtual async Task<byte[]> DecryptAsync(byte[] key, Stream cipher)
        {
            using MemoryStream ms = new();
            await cipher.CopyToAsync(ms);

            ms.Seek(0, SeekOrigin.Begin);

            return Decrypt(key, ms.ToArray());
        }

        public virtual byte[] Encrypt<T>(byte[] key, T message) where T : unmanaged => Encrypt(key, message.BinaryCast());

        public virtual T Decrypt<T>(byte[] key, byte[] cipher) where T : unmanaged => Decrypt(key, cipher).BinaryCast<T>();

        public virtual byte[] Encrypt<T>(string key, T message) where T : unmanaged => Encrypt(key, message, DefaultEncoding);

        public virtual T Decrypt<T>(string key, byte[] cipher) where T : unmanaged => Decrypt<T>(key, cipher, DefaultEncoding);

        public virtual byte[] Encrypt<T>(string key, T message, Encoding encoding) where T : unmanaged => Encrypt(encoding.GetBytes(key), message);

        public virtual T Decrypt<T>(string key, byte[] cipher, Encoding encoding) where T : unmanaged => Decrypt<T>(encoding.GetBytes(key), cipher);

        public override string Encrypt(string key, string message) => Encrypt(key, message, DefaultEncoding);

        public override string Decrypt(string key, string cipher) => Decrypt(key, cipher, DefaultEncoding);

        public virtual string Encrypt(string key, string message, Encoding encoding) => encoding.GetString(Encrypt(encoding.GetBytes(key), encoding.GetBytes(message)));

        public virtual string Decrypt(string key, string cipher, Encoding encoding) => encoding.GetString(Decrypt(encoding.GetBytes(key), encoding.GetBytes(cipher)));
    }

    public abstract class BlockCipher<T>
        : BinaryCipher
        where T : BlockCipher<T>
    {
        public BlockCipherMode Mode { get; }


        protected BlockCipher(BlockCipherMode mode) => Mode = mode;

        public sealed override byte[] Encrypt(byte[] key, byte[] message) => Mode.Encrypt((T)this, key, message);

        public sealed override byte[] Decrypt(byte[] key, byte[] cipher) => Mode.Decrypt((T)this, key, cipher);

        internal protected abstract byte[] _encrypt(byte[] key, byte[] message);

        internal protected abstract byte[] _decrypt(byte[] key, byte[] cipher);
    }

    public abstract class BlockCipherMode
    {
        public int BlockSize { get; }


        protected BlockCipherMode(int blocksize) => BlockSize = blocksize;

        protected byte[] ProcessBlocks(byte[] input, Func<byte[], int, byte[]> func, bool parallel)
        {
            int bs = BlockSize;
            int l = input.Length;
            int bc = (int)Math.Ceiling(l / (double)bs);
            byte[] output = new byte[l];

            void round(int i)
            {
                i *= bs;

                byte[] iblock = i < l - bs ? input[i..(i + bs)] : input[i..];
                byte[] oblock = func(iblock, i);

                Array.Copy(oblock, 0, output, i, oblock.Length);
            }

            if (parallel)
                Parallel.For(0, bc, round);
            else
                for (int i = 0; i < bc; ++i)
                    round(i);

            return output;
        }

        public abstract byte[] Encrypt<T>(T encrpytion, byte[] key, byte[] message) where T : BlockCipher<T>;

        public abstract byte[] Decrypt<T>(T encrpytion, byte[] key, byte[] cipher) where T : BlockCipher<T>;

        public static ElectronicCodebookMode ECB(int blocksize) => new(blocksize);

        public static CipherBlockChainingMode CBC(int blocksize, byte[] iv) => new(blocksize, iv);

        public static CounterMode CTR(int blocksize, byte[] iv) => new(blocksize, iv);

        // public static ElectronicCodebookMode GCM(int blocksize, byte[] iv);


        public sealed class ElectronicCodebookMode
            : BlockCipherMode
        {
            public ElectronicCodebookMode(int blocksize)
                : base(blocksize)
            {
            }

            public override byte[] Encrypt<T>(T encrpytion, byte[] key, byte[] message) => ProcessBlocks(message, (block, _) => encrpytion._encrypt(key, block), true);

            public override byte[] Decrypt<T>(T encrpytion, byte[] key, byte[] cipher) => ProcessBlocks(cipher, (block, _) => encrpytion._decrypt(key, block), true);
        }

        public sealed class CipherBlockChainingMode
            : BlockCipherMode
        {
            public byte[] InitializationVector { get; }


            public CipherBlockChainingMode(int blocksize, byte[]? iv)
                : base(blocksize) => InitializationVector = iv ?? new byte[blocksize];

            public override byte[] Encrypt<T>(T encrpytion, byte[] key, byte[] message)
            {
                byte[] iv = InitializationVector;

                return ProcessBlocks(message, (block, _) =>
                {
                    block = iv.XOR(block);
                    block = encrpytion._encrypt(key, block);

                    return iv = block;
                }, false);
            }

            public override byte[] Decrypt<T>(T encrpytion, byte[] key, byte[] cipher)
            {
                int bs = BlockSize;

                return ProcessBlocks(cipher, (block, i) =>
                {
                    block = encrpytion._decrypt(key, block);

                    byte[] iv = i < bs ? InitializationVector : cipher[(i - bs)..i];

                    return block.XOR(iv);
                }, false);
            }
        }

        public unsafe sealed class CounterMode
            : BlockCipherMode
        {
            public byte[] InitializationVector { get; }


            public CounterMode(int blocksize, byte[]? iv)
                : base(blocksize) => InitializationVector = iv ?? new byte[blocksize];

            public override byte[] Encrypt<T>(T encrpytion, byte[] key, byte[] message) => ProcessBlocks(message, (block, i) =>
            {
                byte[] iv = new byte[BlockSize];

                fixed (byte* ptr = iv)
                    *(int*)ptr ^= i;

                iv = encrpytion._encrypt(key, iv);

                return block.XOR(iv);
            }, true);

            public override byte[] Decrypt<T>(T encrpytion, byte[] key, byte[] cipher) => ProcessBlocks(cipher, (block, i) =>
            {
                byte[] iv = new byte[BlockSize];

                fixed (byte* ptr = iv)
                    *(int*)ptr ^= i;

                iv = encrpytion._decrypt(key, iv);

                return block.XOR(iv);
            }, true);
        }

        //public sealed class GaloisCounterMode
        //    : BlockChiffreMode
        //{
        //    public byte[] InitializationVector { get; }


        //    public GaloisCounterMode(int blocksize, byte[]? iv)
        //        : base(blocksize) => InitializationVector = iv ?? new byte[blocksize];

        //    public override byte[] Encrypt<T>(T encrpytion, byte[] key, byte[] message) => ProcessBlocks(message, (block, i) =>
        //    {
        //    }, false);

        //    public override byte[] Decrypt<T>(T encrpytion, byte[] key, byte[] cipher) => ProcessBlocks(cipher, (block, i) =>
        //    {
        //    }, true);
        //}
    }

    public static class CipherExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Encrypt(this string message, StringCipher algorithm, string key) => algorithm.Encrypt(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Decrypt(this string cipher, StringCipher algorithm, string key) => algorithm.Decrypt(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Encrypt(this byte[] message, BinaryCipher algorithm, byte[] key) => algorithm.Encrypt(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decrypt(this byte[] cipher, BinaryCipher algorithm, byte[] key) => algorithm.Decrypt(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Encrypt(this Stream message, BinaryCipher algorithm, byte[] key) => algorithm.Encrypt(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decrypt(this Stream cipher, BinaryCipher algorithm, byte[] key) => algorithm.Decrypt(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<byte[]> EncryptAsync(this Stream message, BinaryCipher algorithm, byte[] key) => await algorithm.EncryptAsync(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<byte[]> DecryptAsync(this Stream cipher, BinaryCipher algorithm, byte[] key) => await algorithm.DecryptAsync(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Encrypt<T>(this T message, BinaryCipher algorithm, byte[] key) where T : unmanaged => algorithm.Encrypt(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Decrypt<T>(this byte[] cipher, BinaryCipher algorithm, byte[] key) where T : unmanaged => algorithm.Decrypt<T>(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Encrypt<T>(this T message, BinaryCipher algorithm, string key) where T : unmanaged => algorithm.Encrypt(key, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Decrypt<T>(this byte[] cipher, BinaryCipher algorithm, string key) where T : unmanaged => algorithm.Decrypt<T>(key, cipher);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Encrypt<T>(this T message, BinaryCipher algorithm, string key, Encoding encoding) where T : unmanaged => algorithm.Encrypt(key, message, encoding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Decrypt<T>(this byte[] cipher, BinaryCipher algorithm, string key, Encoding encoding) where T : unmanaged => algorithm.Decrypt<T>(key, cipher, encoding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Encrypt(this string message, BinaryCipher algorithm, string key, Encoding encoding) => algorithm.Encrypt(key, message, encoding);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Decrypt(this string cipher, BinaryCipher algorithm, string key, Encoding encoding) => algorithm.Decrypt(key, cipher, encoding);
    }
}
