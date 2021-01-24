using Microsoft.VisualStudio.TestTools.UnitTesting;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public sealed class ScalarTests
        : UnitTestRunner
    {
        [TestMethod]
        public void Test_00__zero()
        {
            Assert.AreEqual<Scalar>(Scalar.Zero, default);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0L);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0UL);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0U);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0d);
            Assert.AreEqual<Scalar>(Scalar.Zero, 0f);
            Assert.AreEqual<Scalar>(Scalar.Zero, (Scalar)0m);
            Assert.IsTrue(Scalar.Zero.IsZero);
        }

        [TestMethod]
        public void Test_01__one()
        {
            Assert.AreEqual<Scalar>(Scalar.One, 1);
            Assert.IsFalse(Scalar.One.IsZero);
            Assert.IsTrue(Scalar.One.IsIdentity);
            Assert.IsTrue(Scalar.One.IsOne);
        }

        [TestMethod]
        public void Test_02__infinity()
        {
            Assert.AreEqual<Scalar>(Scalar.NegativeInfinity, double.NegativeInfinity);
            Assert.AreEqual<Scalar>(Scalar.PositiveInfinity, double.PositiveInfinity);
            Assert.AreEqual<Scalar>(Scalar.NaN, double.NaN);
            Assert.AreEqual<Scalar>(Scalar.NegativeInfinity, float.NegativeInfinity);
            Assert.AreEqual<Scalar>(Scalar.PositiveInfinity, float.PositiveInfinity);
            Assert.AreEqual<Scalar>(Scalar.NaN, float.NaN);
            Assert.AreEqual<Scalar>(Scalar.Zero, Scalar.ComputationalEpsilon / 2);
        }

        [TestMethod]
        public void Test_03__parse()
        {
            Assert.AreEqual<Scalar>((Scalar)"3", 3);
            Assert.AreEqual<Scalar>((Scalar)"3.14", 3.14);
            Assert.AreEqual<Scalar>((Scalar)"-3.14", -3.14);
            Assert.AreEqual<Scalar>((Scalar)"-.14", -.14);
            Assert.AreEqual<Scalar>((Scalar)"+3.14", 3.14);
            Assert.AreEqual<Scalar>((Scalar)"+.14", .14);
            Assert.AreEqual<Scalar>((Scalar)"-3.14e-1", -.314);
            Assert.AreEqual<Scalar>((Scalar)"+.1e1", 1);
            Assert.AreEqual<Scalar>((Scalar)"+π", Scalar.Pi);
            Assert.AreEqual<Scalar>((Scalar)"τ", Scalar.Tau);
            Assert.AreEqual<Scalar>((Scalar)"-2pi", -2 * Scalar.Pi);
            Assert.AreEqual<Scalar>((Scalar)"3.1π", 3.1 * Scalar.Pi);
        }

        [TestMethod]
        [TestWith(-1, 1, 0)]
        [TestWith(10, -.1, 9.9)]
        [TestWith(double.PositiveInfinity, 0, double.PositiveInfinity)]
        public void Test_04__addition(double x, double y, double sum) => Assert.AreEqual<Scalar>(sum, (Scalar)x + (Scalar)y);

        [TestMethod]
        public void Test_05__basics()
        {
            Scalar s1 = 420.88;
            Scalar s2 = -3.15e-1;

            Assert.AreEqual(s1 - s2, s1 - (+s2));
            Assert.AreEqual(s1 - s2, s1 + (-s2));
            Assert.AreEqual(s1 + s2, s1 - (-s2));
            Assert.AreEqual(s1 + s2, s2 + s1);
            Assert.AreNotEqual(s1 - s2, s2 - s1);
            Assert.AreEqual(s1 * s2, s2 * s1);
            Assert.AreEqual(s1 * s2.Inverse, s1 / s2);
        }


    }
}
