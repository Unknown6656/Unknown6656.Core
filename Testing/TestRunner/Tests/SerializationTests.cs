using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.IO;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.IO;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public unsafe sealed class SerializationTests
        : UnitTestRunner
    {
        [TestMethod]
        [TestWith(42)]
        [TestWith(-14.88)]
        [TestWith(315UL)]
        [TestWith((byte)0xf9)]
        [TestWith('τ')]
        public void Test_00__native<T>(T data)
            where T : unmanaged
        {
            DataStream from = DataStream.FromUnmanaged(data);
            string b64 = from.ToBase64();
            string hex = from.ToHexString();
            byte[] arr = from.ToBytes();

            T t1 = DataStream.FromBytes(arr).ToUnmanaged<T>();
            T t2 = DataStream.FromBase64(b64).ToUnmanaged<T>();
            T t3 = DataStream.FromHex(hex).ToUnmanaged<T>();

            Assert.AreEqual(data, t1);
            Assert.AreEqual(data, t2);
            Assert.AreEqual(data, t3);
        }

        [TestMethod]
        public void Test_01__string()
        {
            const string data = "This is a test string containing unicode characters and null bytes: «🌄» «\0».";
            DataStream from = DataStream.FromString(data);

            string str = from.ToString();
            string b64 = from.ToBase64();

            Assert.AreEqual(data, str);
            Assert.AreEqual("VGhpcyBpcyBhIHRlc3Qgc3RyaW5nIGNvbnRhaW5pbmcgdW5pY29kZSBjaGFyYWN0ZXJzIGFuZCBudWxsIGJ5dGVzOiDCq/CfjITCuyDCqwDCuy4=", b64);
        }

        [TestMethod]
        public void Test_02__matrix()
        {
            Matrix3 src = (
                1, 2, 3,
                4, 5, 6,
                7, 8, 9
            );
            MemoryStream ms = DataStream.FromUnmanaged(src).ToStream();
            Matrix3 dst1;
            Scalar* dst2 = stackalloc Scalar[9];

            DataStream.FromStream(ms).ToPointer(&dst1);
            ms.Seek(0, SeekOrigin.Begin);
            DataStream.FromStream(ms).ToPointer(dst2);

            Assert.AreEqual(src, dst1);
            Assert.AreEqual(src, new Matrix3(dst2));
        }
    }
}
