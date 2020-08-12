using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics;
using Unknown6656.Common;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public sealed class CryptographyTest
        : UnitTestRunner
    {
        private static readonly Mathematics.Numerics.Random RANDOM = new XorShift();
        private static readonly string PASS = "-=secure password=-";
        private static readonly byte[] IV = { 0x14, 0xab, 0x88, 0xf7 };
        private static readonly string DATA_1 = "0123"; // "Hello, World! This is a test message!";
        private static readonly (Matrix3, Vector5, DateTime) DATA_2 = (
            new Matrix3(
                1, 2, 3,
                4, 5, 6,
                7, 8, 9
            ),
            new Vector5(-4, 7, .8, 44, 7),
            DateTime.UtcNow
        );


        [TestMethod]
        public void Test_00__xor()
        {
            byte[] arr1 = RANDOM.NextBytes(64);
            byte[] arr2 = RANDOM.NextBytes(64);
            byte[] enc = arr1.XOR(arr2);
            byte[] dec = enc.XOR(arr2);

            AssertExtensions.AreSequentialEqual(arr1, dec);
        }

        [TestMethod]
        public (string, string) Test_01__ECB()
        {
            string cipher1 = test_blockmode(BlockCipherMode.ECB(128));
            string cipher2 = test_blockmode(BlockCipherMode.ECB(4));

            Assert.AreNotEqual(cipher1, cipher2);

            return (cipher1, cipher2);
        }

        [TestMethod]
        public string Test_02__CBC() => test_blockmode(BlockCipherMode.CBC(IV.Length, IV));

        [TestMethod]
        public string Test_03__CTR() => test_blockmode(BlockCipherMode.CTR(IV.Length, IV));

        [TestMethod]
        public void Test_04__blockmodes()
        {
            (string c1, string c2) = Test_01__ECB();
            string c3 = Test_02__CBC();
            string c4 = Test_03__CTR();

            Assert.AreEqual(4, new[] { c1, c2, c3, c4 }.Distinct().Count());
        }

        [TestMethod]
        public void Test_05__complex_data()
        {
            var xor = new Test_01__xor_encrypt(BlockCipherMode.CTR(IV.Length, IV));
            var enc = xor.Encrypt(PASS, DATA_2);
            var dec = xor.Decrypt<(Matrix3, Vector5, DateTime)>(PASS, enc);

            Assert.AreEqual(dec, DATA_2);
        }

        [TestMethod]
        public byte[] Test_06__OEAEP_MD5() => test_oaep(HashFunctions.MD5);

        [TestMethod]
        public byte[] Test_07__OEAEP_SHA1() => test_oaep(HashFunctions.SHA1);

        [TestMethod]
        public byte[] Test_08__OEAEP_SHA256() => test_oaep(HashFunctions.SHA256);

        [TestMethod]
        public byte[] Test_09__OEAEP_SHA384() => test_oaep(HashFunctions.SHA384);

        [TestMethod]
        public byte[] Test_10__OEAEP_SHA512() => test_oaep(HashFunctions.SHA512);

        [TestMethod]
        public void Test_11__IND_CPA()
        {
            const int count = 10;
            var oaep = HashFunctions.MD5.CreateOAEP(RANDOM);
            var enc = Enumerable.Repeat(DATA_1, count)
                                .Select(s => Convert.ToBase64String(oaep.Pad(s)))
                                .Distinct()
                                .ToArray();

            Assert.AreEqual(count, enc.Length);
        }

        [TestMethod]
        public void Test_12__vigenere()
        {
            Vigenere vig = new Vigenere("abcdefghijklmnopqrstuvwxyz ");
            string plain = "this is a message";
            string passw = "pass";

            string cipher = vig.Encrypt(passw, plain);
            string reconstructed = vig.Decrypt(passw, cipher);

            Assert.AreEqual(plain, reconstructed);

            plain = "eeeeeeeeeee";
            cipher = vig.Encrypt(passw, plain);

            int len = passw.Length;

            for (int i = 1; i < len; ++i)
                Assert.AreNotEqual(cipher[0], cipher[i]);

            Assert.AreEqual(cipher[..len], cipher[len..(2 * len)]);
        }





        private string test_blockmode(BlockCipherMode mode)
        {
            BinaryCipher xor = new Test_01__xor_encrypt(mode);
            string enc = DATA_1.Encrypt(xor, PASS, BytewiseEncoding.Instance);
            string dec = enc.Decrypt(xor, PASS);

            Assert.AreEqual(DATA_1, dec);

            return enc;
        }

        private byte[] test_oaep<T>(T hash) where T : HashFunction<T>
        {
            var oaep = hash.CreateOAEP(RANDOM);
            var enc = oaep.Pad(DATA_2);
            var dec = oaep.UnpadData<(Matrix3, Vector5, DateTime)>(enc);

            Assert.AreEqual(DATA_2, dec);

            return enc;
        }

        private sealed class Test_01__xor_encrypt
            : BlockCipher<Test_01__xor_encrypt>
        {
            public Test_01__xor_encrypt(BlockCipherMode mode) : base(mode) { }
            protected override byte[] _decrypt(byte[] key, byte[] cipher) => Encrypt(key, cipher);
            protected override byte[] _encrypt(byte[] key, byte[] message)
            {
                byte[] output = new byte[message.Length];

                for (int i = 0; i < message.Length; ++i)
                    output[i] = (byte)(message[i] ^ key[i % key.Length]);

                return output;
            }
        }
    }
}
