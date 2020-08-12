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
            To to = From.Unmanaged(data).To;
            string b64 = to.Base64();
            string hex = to.Hex();
            byte[] arr = to.Bytes;

            T t1 = From.Bytes(arr).To.Unmanaged<T>();
            T t2 = From.Base64(b64).To.Unmanaged<T>();
            T t3 = From.Hex(hex).To.Unmanaged<T>();

            Assert.AreEqual(data, t1);
            Assert.AreEqual(data, t2);
            Assert.AreEqual(data, t3);
        }

        [TestMethod]
        public void Test_01__string()
        {
            const string data = "This is a test string containing unicode characters and null bytes: «🌄» «\0».";
            To to = From.String(data).To;

            string str = to.String();
            string b64 = to.Base64();

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
            MemoryStream ms = From.Unmanaged(src).To.Stream();
            Matrix3 dst1;
            Scalar* dst2 = stackalloc Scalar[9];

            From.Stream(ms).To.Pointer(&dst1);
            ms.Seek(0, SeekOrigin.Begin);
            From.Stream(ms).To.Pointer(dst2);

            Assert.AreEqual(src, dst1);
            Assert.AreEqual(src, new Matrix3(dst2));
        }
    }
}
