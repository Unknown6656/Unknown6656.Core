using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public sealed class ComplexTests
        : UnitTestRunner
    {
        [TestMethod]
        public void Test_00__zero()
        {
            Assert.AreEqual<Complex>(Complex.Zero, default);
            Assert.AreEqual<Complex>(Complex.Zero, 0L);
            Assert.AreEqual<Complex>(Complex.Zero, 0UL);
            Assert.AreEqual<Complex>(Complex.Zero, 0);
            Assert.AreEqual<Complex>(Complex.Zero, 0U);
            Assert.AreEqual<Complex>(Complex.Zero, 0d);
            Assert.AreEqual<Complex>(Complex.Zero, 0f);
            Assert.AreEqual<Complex>(Complex.Zero, Scalar.Zero);
            Assert.IsTrue(Complex.Zero.IsZero);
        }

        // TODO

        [TestMethod]
        public void Test_10__parsing()
        {
            foreach ((string s, Complex expected) in new (string, Complex)[]
            {
                ("0", Complex.Zero),
                ("+0", Complex.Zero),
                ("-0", Complex.Zero),
                ("0i", Complex.Zero),
                ("0π", Complex.Zero),
                ("0pi", Complex.Zero),
                ("-0i", Complex.Zero),
                ("1", 1),
                ("-1", -1),
                ("1.23", 1.23),
                ("-1.23", -1.23),
                (".23", .23),
                ("-.23", -.23),
                ("1.23e1", 1.23e1),
                ("1.23e+1", 1.23e+1),
                ("1.23e-1", 1.23e-1),
                ("+1.23e-1", 1.23e-1),
                ("-1.23e-1", -1.23e-1),
                ("e", Scalar.e),
                ("-e", -Scalar.e),
                ("tau", Scalar.Tau),
                ("τ", Scalar.Tau),
                ("-τ", -Scalar.Tau),
                ("πi", Scalar.Pi * Complex.i),
                ("i", Complex.i),
                ("-i", -Complex.i),
                ("1i", Complex.i),
                ("-1i", -Complex.i),
                (".1i", .1 * Complex.i),
                ("-.1i", -.1 * Complex.i),
                (".1*i", .1 * Complex.i),
                ("-.1*i", -.1 * Complex.i),
                ("i*.1", .1 * Complex.i),
                ("-i*.1", -.1 * Complex.i),
                ("1e-1i", .1 * Complex.i),
                ("-1e-1i", -.1 * Complex.i),
                ("i1e-1", .1 * Complex.i),
                ("-i1e-1", -.1 * Complex.i),
                ("1+i", new Complex(1, 1)),
                ("1-i", new Complex(1, -1)),
                ("+1+i", new Complex(1, 1)),
                ("-1+i", new Complex(-1, 1)),
                ("+1-i", new Complex(1, -1)),
                ("-1-i", new Complex(-1, -1)),
                ("1-2i", new Complex(1, -2)),
                ("-2+3i", new Complex(-2, 3)),
                ("-1e-2+2e+2i", new Complex(-1e-2, 2e+2)),
                ("42e-1pii", new Complex(0, 42e-1 * Scalar.Pi)),
                ("3phi-2ei", new Complex(3 * Scalar.GoldenRatio, -2 * Scalar.e)),
            })
                if (Complex.TryParse(s, out Complex actual))
                    Assert.AreEqual(expected, actual, $"Parsing string '{s}'.");
                else
                    Assert.Fail($"Unable to parse '{s}' as a complex number.");
        }
    }
}
