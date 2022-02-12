using Microsoft.VisualStudio.TestTools.UnitTesting;

using Unknown6656.Mathematics.LinearAlgebra;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public sealed class VectorTests
        : UnitTestRunner
    {
        [TestMethod]
        public void Test_00__zero()
        {
            Assert.IsTrue(Vector2.Zero.IsZero);
            Assert.IsTrue(Vector3.Zero.IsZero);
            Assert.IsTrue(Vector4.Zero.IsZero);
            Assert.IsTrue(Vector5.Zero.IsZero);
            Assert.IsTrue(Vector6.Zero.IsZero);
            Assert.IsTrue(Vector7.Zero.IsZero);
            Assert.IsTrue(Vector8.Zero.IsZero);
            Assert.IsTrue(Vector9.Zero.IsZero);
            Assert.IsTrue(Vector10.Zero.IsZero);

            Assert.IsFalse(Vector2.Zero.IsPositive);
            Assert.IsFalse(Vector3.Zero.IsPositive);
            Assert.IsFalse(Vector4.Zero.IsPositive);
            Assert.IsFalse(Vector5.Zero.IsPositive);
            Assert.IsFalse(Vector6.Zero.IsPositive);
            Assert.IsFalse(Vector7.Zero.IsPositive);
            Assert.IsFalse(Vector8.Zero.IsPositive);
            Assert.IsFalse(Vector9.Zero.IsPositive);
            Assert.IsFalse(Vector10.Zero.IsPositive);

            Assert.IsFalse(Vector2.Zero.IsNegative);
            Assert.IsFalse(Vector3.Zero.IsNegative);
            Assert.IsFalse(Vector4.Zero.IsNegative);
            Assert.IsFalse(Vector5.Zero.IsNegative);
            Assert.IsFalse(Vector6.Zero.IsNegative);
            Assert.IsFalse(Vector7.Zero.IsNegative);
            Assert.IsFalse(Vector8.Zero.IsNegative);
            Assert.IsFalse(Vector9.Zero.IsNegative);
            Assert.IsFalse(Vector10.Zero.IsNegative);
        }

        [TestMethod]
        public void Test_01__default()
        {
            Assert.AreEqual(Vector2.Zero, default);
            Assert.AreEqual(Vector3.Zero, default);
            Assert.AreEqual(Vector4.Zero, default);
            Assert.AreEqual(Vector5.Zero, default);
            Assert.AreEqual(Vector6.Zero, default);
            Assert.AreEqual(Vector7.Zero, default);
            Assert.AreEqual(Vector8.Zero, default);
            Assert.AreEqual(Vector9.Zero, default);
            Assert.AreEqual(Vector10.Zero, default);
        }

        [TestMethod]
        public void Test_02__unit_vectors()
        {
            Assert.IsFalse(Vector2.UnitX.IsPositive);
            Assert.IsFalse(Vector2.UnitY.IsPositive);
            Assert.IsFalse(Vector3.UnitX.IsPositive);
            Assert.IsFalse(Vector3.UnitY.IsPositive);
            Assert.IsFalse(Vector3.UnitZ.IsPositive);
            Assert.IsFalse(Vector4.UnitX.IsPositive);
            Assert.IsFalse(Vector4.UnitY.IsPositive);
            Assert.IsFalse(Vector4.UnitZ.IsPositive);
            Assert.IsFalse(Vector4.UnitW.IsPositive);
            Assert.IsFalse(Vector5.UnitX.IsPositive);
            Assert.IsFalse(Vector5.UnitY.IsPositive);
            Assert.IsFalse(Vector5.UnitZ.IsPositive);
            Assert.IsFalse(Vector5.UnitV.IsPositive);
            Assert.IsFalse(Vector5.UnitW.IsPositive);
            Assert.IsFalse(Vector6.UnitX.IsPositive);
            Assert.IsFalse(Vector6.UnitY.IsPositive);
            Assert.IsFalse(Vector6.UnitZ.IsPositive);
            Assert.IsFalse(Vector6.UnitU.IsPositive);
            Assert.IsFalse(Vector6.UnitV.IsPositive);
            Assert.IsFalse(Vector6.UnitW.IsPositive);
        }

        [TestMethod]
        public void Test_03__unit_vectors()
        {
            Assert.AreEqual(Vector2.UnitX, Vector2.UnitX.Normalized);
            Assert.AreEqual(Vector2.UnitY, Vector2.UnitY.Normalized);
            Assert.AreEqual(Vector3.UnitX, Vector3.UnitX.Normalized);
            Assert.AreEqual(Vector3.UnitY, Vector3.UnitY.Normalized);
            Assert.AreEqual(Vector3.UnitZ, Vector3.UnitZ.Normalized);
            Assert.AreEqual(Vector4.UnitX, Vector4.UnitX.Normalized);
            Assert.AreEqual(Vector4.UnitY, Vector4.UnitY.Normalized);
            Assert.AreEqual(Vector4.UnitZ, Vector4.UnitZ.Normalized);
            Assert.AreEqual(Vector4.UnitW, Vector4.UnitW.Normalized);
            Assert.AreEqual(Vector5.UnitX, Vector5.UnitX.Normalized);
            Assert.AreEqual(Vector5.UnitY, Vector5.UnitY.Normalized);
            Assert.AreEqual(Vector5.UnitZ, Vector5.UnitZ.Normalized);
            Assert.AreEqual(Vector5.UnitV, Vector5.UnitV.Normalized);
            Assert.AreEqual(Vector5.UnitW, Vector5.UnitW.Normalized);
            Assert.AreEqual(Vector6.UnitX, Vector6.UnitX.Normalized);
            Assert.AreEqual(Vector6.UnitY, Vector6.UnitY.Normalized);
            Assert.AreEqual(Vector6.UnitZ, Vector6.UnitZ.Normalized);
            Assert.AreEqual(Vector6.UnitU, Vector6.UnitU.Normalized);
            Assert.AreEqual(Vector6.UnitV, Vector6.UnitV.Normalized);
            Assert.AreEqual(Vector6.UnitW, Vector6.UnitW.Normalized);
        }

        [TestMethod]
        public void Test_04__deconstruction()
        {
            Assert.AreEqual<Vector3>(new Vector3(1, 2, 3), (1, 2, 3));
            Assert.AreEqual<(Scalar, Scalar, Scalar)>((1, 2, 3), new Vector3(1, 2, 3));
        }

        [TestMethod]
        public void Test_05__addition()
        {
            Vector3 v1 = (0, -1, 4);
            Vector3 v2 = (8, 2, -.5);
            Vector3 r = (8, 1, 3.5);

            Assert.AreEqual(r, v1 + v2);
            Assert.AreEqual(r, v2 + v1);
            Assert.AreEqual(r, v1.Add(v2));
            Assert.AreEqual(r, v2.Add(v1));
            Assert.AreEqual<Vector3>((0, -1, 4), v1);
            Assert.AreEqual<Vector3>((8, 2, -.5), v2);
        }

        [TestMethod]
        public void Test_06__subtraction()
        {
            Vector3 v1 = (0, -1, 4);
            Vector3 v2 = (8, 2, -.5);
            Vector3 r = (-8, -3, 4.5);

            Assert.AreEqual(r, v1 - v2);
            Assert.AreNotEqual(v1 - v2, v2 - v1);
            Assert.AreEqual(r, v1.Subtract(v2));
            Assert.AreEqual<Vector3>((0, -1, 4), v1);
            Assert.AreEqual<Vector3>((8, 2, -.5), v2);
            Assert.AreEqual(-v1, Vector3.Zero - v1);
        }

        [TestMethod]
        public void Test_07__scaling()
        {
            Scalar f = 2;
            Vector3 v = (0, -1.5, 4);
            Vector3 r = (0, -3, 8);

            Assert.AreEqual(r, v * f);
            Assert.AreEqual(r, f * v);
            Assert.AreEqual(r, v.Multiply(f));
            Assert.AreEqual(r, v.ComponentwiseMultiply((f, f, f)));
            Assert.AreEqual(r, v.ComponentwiseMultiply(new Vector3(f)));
            Assert.AreEqual(f * v, v + v);
            Assert.AreEqual(v.Normalized, (f * v).Normalized);
            Assert.AreEqual(v.Length * f, (f * v).Length);
        }

        [TestMethod]
        public void Test_08__inner_product()
        {
            Vector3 v1 = (0, -1, 4);
            Vector3 v2 = (8, 2, -.5);
            Scalar r = -4;

            Assert.AreEqual(r, v1 * v2);
            Assert.AreEqual(r, v2 * v1);
            Assert.AreEqual(r, v1.Dot(v2));
            Assert.AreEqual(r, v2.Dot(v1));
        }

        [TestMethod]
        public void Test_09__outer_product()
        {
            Vector3 v1 = (0, -1, 4);
            Vector3 v2 = (8, 2, -.5);
            Matrix3 r = (
                0, 0, 0,
                -8, -2, .5,
                32, 8, -2
            );

            Assert.AreEqual(r, v1.OuterProduct(v2));
            Assert.AreNotEqual(r, v2.OuterProduct(v1));
        }

        [TestMethod]
        public void Test_10__cross_product()
        {
            Vector3 v1 = (0, -1, 4);
            Vector3 v2 = (8, 2, -.5);
            Vector3 r = (-7.5, 32, 8);

            Assert.AreEqual(r, v1.Cross(v2));
            Assert.AreNotEqual(r, v2.Cross(v1));
        }

        [TestMethod]
        public void Test_11__length()
        {
            Vector3 v = (8, 2, -.5);
            Scalar l = 8.2613558209291522;

            Assert.AreEqual(l, v.Length);
            Assert.AreEqual(l * 2, (v * 2).Length);
            Assert.AreEqual(l, (-v).Length);
            Assert.AreEqual<Scalar>(1, v.Normalized.Length);
            Assert.AreEqual(v * v, v.SquaredLength);
        }

        [TestMethod]
        public void Test_12__basic_properties()
        {
            Vector3 v = (8, 2, -.5);

            Assert.IsFalse(v.IsZero);
            Assert.IsFalse(v.IsPositive);
            Assert.IsFalse(v.IsNegative);
            Assert.IsTrue(v.Length > 0);
            Assert.AreEqual<Scalar>(8, v.CoefficientMax);
            Assert.AreEqual<Scalar>(-.5, v.CoefficientMin);
            Assert.AreEqual<Scalar>(9.5, v.CoefficientSum);
            Assert.AreEqual<Scalar>(8, v.X);
            Assert.AreEqual<Scalar>(2, v.Y);
            Assert.AreEqual<Scalar>(-.5, v.Z);
            Assert.AreEqual<Scalar>(3, v.Size);
        }

        // Assert.IsFalse(v.IsLinearIndependent(f * v));
    }
}
