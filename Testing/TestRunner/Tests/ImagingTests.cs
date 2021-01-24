using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Drawing;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public unsafe sealed class ImagingTests
        : UnitTestRunner
    {
        public static Bitmap IMG_1;

        public override void Test_StaticInit()
        {
            //byte[] bytes = Resources.park_bench;
            //using MemoryStream ms = new MemoryStream(bytes);
            //
            //IMG_1 = new Bitmap(ms);
            IMG_1 = (Bitmap)Image.FromFile("./Resources/park-bench.jpg");
        }

        public override void Test_StaticCleanup() => IMG_1.Dispose();

        [TestMethod]
        public void Test_00__create()
        {
            WritableMatrixNM<Vector4> mat = new HDRBitmap(IMG_1);

            mat = mat.Transposed;
            mat = mat[100..200, 100..200];
            //mat = mat.NormalizeMinMax();

            HDRBitmap bmp2 = new HDRBitmap(mat);
            Bitmap outp = bmp2.ToBitmap();
        }

        [TestMethod]
        public void Test_01__colors()
        {
            RGBAColor px = 0xffff0000;
            Vector4 rgba = px;
            Vector3 rgb = px;

            Assert.AreEqual<Vector3>(rgb, (1, 0, 0));
            Assert.AreEqual<Vector4>(rgba, (1, 0, 0, 1));
            Assert.AreEqual<RGBAColor>(rgb, 0xffff0000);
            Assert.AreEqual<RGBAColor>(rgba, 0xffff0000);
        }

        [TestMethod]
        public void Test_05__convolution()
        {
            var bmp = IMG_1.ToARGB32();
            var res = bmp.ApplyEffect(new SingleConvolutionEffect(new Matrix3(
                0, 0, 0,
                0, 1, 0,
                0, 0, 0
            )));

            res.Save("conv.png");
        }
    }
}
