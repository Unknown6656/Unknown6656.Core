using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public unsafe sealed class PolynomialTests
        : UnitTestRunner
    {
        [TestMethod]
        public void Test_00__construct()
        {
            Polynomial p1 = new Polynomial(-1, 2, 3); // 3x² + 2x - 1
            Polynomial p2 = new Polynomial(new VectorN(-1, 2, 3));
            Polynomial p3 = new Vector3(-1, 2, 3).ToPolynomial();
            Polynomial p4 = Polynomial.Parse("3x² + 2x - 1");
            Polynomial p5 = Polynomial.Parse("+3*x^2 + 2*x - 1");

            Assert.AreEqual("3x² + 2x - 1", p1.ToString());
            Assert.AreEqual(p1, p2);
            Assert.AreEqual(p1, p3);
            Assert.AreEqual(p1, p4);
            Assert.AreEqual(p1, p5);
        }
    }
}
