using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;


namespace Unknown6656.Testing.Tests
{
    [TestClass]
    public sealed unsafe class MatrixTests
        : UnitTestRunner
    {
        [TestMethod]
        public void Test_00__construct()
        {
            Matrix2 m = new Matrix2(
                0, 1,
                2, 3
            );

            Matrix2 m1 = new Matrix2(m);
            Matrix2 m2 = new Matrix2(&m);
            Matrix2 m3 = new Matrix2(new Scalar[2, 2]
            {
                { 0, 2 },
                { 1, 3 },
            });
            Matrix2 m4 = new Matrix2(new Scalar[4]
            {
                0, 1,
                2, 3
            });
            Matrix2 m5 = new Matrix2(new Scalar[2][]
            {
                new Scalar[2] { 0, 2 },
                new Scalar[2] { 1, 3 },
            });
            Matrix2 m6 = new Matrix2(m.Columns);
            Matrix2 m7 = new Matrix2(new Vector2[]
            {
                new Vector2(0, 2),
                new Vector2(1, 3),
            });
            Matrix2 m8 = new Matrix2(
                new Vector2(0, 2),
                new Vector2(1, 3)
            );
            Matrix2 m9 = new Matrix2(
                0, 1,
                2, 3
            );

            Assert.AreEqual(m, m1);
            Assert.AreEqual(m, m2);
            Assert.AreEqual(m, m3);
            Assert.AreEqual(m, m4);
            Assert.AreEqual(m, m5);
            Assert.AreEqual(m, m6);
            Assert.AreEqual(m, m7);
            Assert.AreEqual(m, m8);
            Assert.AreEqual(m, m9);
        }

        [TestMethod]
        public void Test_01__construct()
        {
            Matrix2 m = new Matrix2(
                0, 1,
                2, 3
            );


            Matrix2 m1 = new Matrix2(
                (0, 2),
                (1, 3)
            );
            Matrix2 m2 = (
                0, 1,
                2, 3
            );
            Matrix2 m3 = new Scalar[2, 2]
            {
                { 0, 2 },
                { 1, 3 }
            };
            Matrix2 m4 = new Scalar[4]
            {
                0, 1,
                2, 3
            };
            Matrix2 m5 = new[]
            {
                new Vector2(0, 2),
                new Vector2(1, 3)
            };

            Assert.AreEqual(m, m1);
            Assert.AreEqual(m, m2);
            Assert.AreEqual(m, m3);
            Assert.AreEqual(m, m4);
            Assert.AreEqual(m, m5);
        }

        [TestMethod]
        public void Test_02__deconstruct()
        {
            Matrix2 m = (
                0, 1,
                2, 3
            );

            var (
                a, b,
                c, d
            ) = m;
            var (c1, c2) = m;
            Scalar[] arr1 = (Scalar[])m;
            Scalar[,] arr2 = m;
            Vector2[] arr3 = m;

            Assert.AreEqual<Scalar>(a, 0);
            Assert.AreEqual<Scalar>(b, 1);
            Assert.AreEqual<Scalar>(c, 2);
            Assert.AreEqual<Scalar>(d, 3);
            Assert.AreEqual(c1, new Vector2(0, 2));
            Assert.AreEqual(c2, new Vector2(1, 3));
            Assert.AreEqual<Scalar>(arr1[0], 0);
            Assert.AreEqual<Scalar>(arr1[1], 1);
            Assert.AreEqual<Scalar>(arr1[2], 2);
            Assert.AreEqual<Scalar>(arr1[3], 3);
            Assert.AreEqual<Scalar>(arr2[0, 0], 0);
            Assert.AreEqual<Scalar>(arr2[1, 0], 1);
            Assert.AreEqual<Scalar>(arr2[0, 1], 2);
            Assert.AreEqual<Scalar>(arr2[1, 1], 3);
            Assert.AreEqual(arr3[0], new Vector2(0, 2));
            Assert.AreEqual(arr3[1], new Vector2(1, 3));
        }

        [TestMethod]
        public void Test_03__matrix_nm()
        {
            MatrixNM m = new MatrixNM(4, 3, new Scalar[]
            {
                0, 1, 2, 3,
                4, 5, 6, 7,
                8, 9, -1, -2
            });

            MatrixNM m1 = new MatrixNM(m);
            MatrixNM m2 = new MatrixNM(m.Coefficients);
            MatrixNM m3 = new MatrixNM(m.ColumnCount, m.RowCount, m.FlattenedCoefficients);
            MatrixNM m4 = new MatrixNM(m.Columns);

            Assert.AreEqual(4, m.ColumnCount);
            Assert.AreEqual(3, m.RowCount);
            Assert.AreEqual(12, m.Coefficients.Length);
            Assert.AreEqual(new VectorN(4, 5, 6, 7), m.Rows[1]);
            Assert.AreEqual(new VectorN(2, 6, -1), m.Columns[^2]);
            Assert.AreEqual(m, m1);
            Assert.AreEqual(m, m2);
            Assert.AreEqual(m, m3);
            Assert.AreEqual(m, m4);

            m = new MatrixNM(3, 3, new Scalar[]
            {
                0, 1, 2,
                3, 4, 5,
                6, 7, 8
            });

            Algebra<Scalar, Polynomial, ScalarMap>.IMatrix m5 = m.Cast();
            MatrixNM m6 = (Matrix3)m5;

            Assert.IsInstanceOfType(m5, typeof(Matrix3));
            Assert.AreEqual(m, m6);
        }

        [TestMethod]
        public void Test_04__regions()
        {
            Matrix4 m = (
                0, 1, 2, 3,
                4, 5, 6, 7,
                8, 9, 0, -1,
                -2, -3, -4, -5
            );

            Assert.AreEqual<Vector4>((0, 4, 8, -2), m[0]);
            Assert.AreEqual<Vector4>((3, 7, -1, -5), m[^1]);
            Assert.AreEqual<Matrix2>((0, 1, 4, 5), m.PrincipalSubmatrices.Sub2);
            Assert.AreEqual<Matrix3>((
                0, 1, 2,
                4, 5, 6,
                8, 9, 0
            ), m.PrincipalSubmatrices.Sub3);
            Assert.AreEqual<Matrix3>((
                0, 1, 3,
                8, 9, -1,
                -2, -3, -5
            ), m.Minors[2, 1]);
            Assert.AreEqual(new MatrixNM(3, 2, new Scalar[]
            {
                1, 2, 3,
                5, 6, 7,
            }), m[1..4, ..2]);
        }

        [TestMethod]
        public void Test_05__characteristics()
        {
            Matrix3 symm = (
                1, 2, 3,
                2, 0, -5,
                3, -5, 6
            );
            Matrix3 diag = (
                1, 0, 0,
                0, 4, 0,
                0, 0, 6
            );
            Matrix3 u_tr = (
                1, 2, 3,
                0, 7, -5,
                0, 0, -4
            );

            Assert.IsTrue(symm.IsSymmetric);
            Assert.IsTrue(diag.IsDiagonal);
            Assert.IsFalse(diag.IsHollow);
            Assert.IsTrue(diag.IsLowerTriangular);
            Assert.IsTrue(diag.IsUpperTriangular);
            Assert.IsTrue(u_tr.IsUpperTriangular);
        }

        [TestMethod]
        public void Test_06__arithmetics()
        {
            Matrix3 A = (
                1, 2, 3,
                0, 7, -5,
                0, 0, -4
            );
            Matrix3 B = (
                1, 2, 3,
                2, 0, -5,
                3, -5, 6
            );

            Assert.AreNotEqual(A, B);
            Assert.AreEqual(A, +A);
            Assert.AreEqual(new Matrix3(
                -1, -2, -3,
                0, -7, 5,
                0, 0, 4
            ), -A);
            Assert.AreEqual(A.AdditiveInverse, -A);
            Assert.AreEqual(new Matrix3(
                2.5, 5, 7.5,
                0, 17.5, -12.5,
                0, 0, -10
            ), A * 2.5);
            Assert.AreEqual(new Matrix3(
                2, 4, 6,
                2, 7, -10,
                3, -5, 2
            ), A + B);
            Assert.AreEqual(A + (-B), A - B);
            Assert.AreEqual(new Matrix3(
                0, 0, 0,
                -2, 7, 0,
                -3, 5, -10
            ), A - B);
            Assert.AreEqual(new Matrix3(
                14, -13, 11,
                -1, 25, -65,
                -12, 20, -24
            ), A * B);
            Assert.IsTrue(Matrix3.Identity.Is(A.Inverse * A, 1e-5));
            Assert.AreEqual(B * A.Inverse, B / A);
        }

        [TestMethod]
        public void Test_07__determinant()
        {
            Matrix3 A = (
                1, 2, 3,
                0, 7, -5,
                0, 0, -4
            );
            Matrix3 B = (
                1, 0, 0,
                0, 4, 0,
                0, 0, 6
            );

            Assert.AreEqual<Scalar>(-28, A.Determinant);
            AssertExtensions.AreSetEqual(new Scalar[] { 1, 7, -4 }, A.Eigenvalues);
            Assert.AreEqual(A.Determinant.Inverse, A.Inverse.Determinant);
            Assert.AreEqual(new Polynomial(24, -34, 11, -1), B.CharacteristicPolynomial);
            Assert.AreEqual(A.Determinant * B.Determinant, (A * B).Determinant);
        }



        // Assert.IsFalse(v.IsLinearIndependent(f * v));
    }
}
