#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Common;

using bint = System.Numerics.BigInteger;

namespace Unknown6656.Mathematics.LinearAlgebra
{
#if DOUBLE_PRECISION
    using __scalar = Double;
#else
    using __scalar = Single;
#endif

    [StructLayout(LayoutKind.Sequential, Size = sizeof(__scalar), Pack = sizeof(__scalar)), NativeCppClass, Serializable, CLSCompliant(false)]
    public unsafe readonly /* ref */ partial struct Scalar
        : IScalar<Scalar>
        , Algebra<Scalar>.IMatrix<Scalar, Scalar>
        , Algebra<Scalar>.IVector<Scalar, Scalar>
        , Algebra<Scalar, Polynomial>.IComposite1D
        , Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>
        , IReadonlyNative<Scalar>
        , IComparable<Scalar>
        , IEquatable<Scalar>
        , IComparable
        , IDisplayable
        , ICloneable
    {
        #region CONSTS AND FIELDS

        /// <inheritdoc cref="IMatrix{M,S}.Size"/>
        public const int Dimension = 1;

        /// <summary>
        /// The default value for <see cref="ComputationalEpsilon"/> (1e-9).
        /// </summary>
        public const double DefaultComputationalEpsilon = 1e-9;

        private static Scalar _cepsilon = DefaultComputationalEpsilon;


        internal static readonly Regex REGEX_BIN = new Regex(@"[+\-]?(?<num>0b[01]+|[01]+b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex REGEX_OCT = new Regex(@"[+\-]?(?<num>0o[0-7]+|[0-7]+o)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex REGEX_HEX = new Regex(@"[+\-]?(?<num>0x[0-9a-f]+|[0-9a-f]+h)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex REGEX_DEC1 = new Regex(@"[+\-]?((?<const>π|pi|τ|tau|φ|phi|e)\*?)?(?<factor>(\d*\.)?\d+(e[+\-]?\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static readonly Regex REGEX_DEC2 = new Regex(@"[+\-]?((?<factor>(\d*\.)?\d+(e[+\-]?\d+)?)\*?)?(?<const>π|pi|τ|tau|φ|phi|e)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex REGEX_SCALAR = new Regex(@"^[+\-]?(0b[01]+|[01]+b|0o[0-7]+|[0-7]+o|0x[0-9a-f]+|[0-9a-f]+h|((π|pi|τ|tau|φ|phi|e)\*?)?((\d*\.)?\d+(e[+\-]?\d+)?)|(((\d*\.)?\d+(e[+\-]?\d+)?)\*?)?(π|pi|τ|tau|φ|phi|e))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        #endregion
        #region STATIC PROPERTIES

#pragma warning disable IDE1006
        public static Scalar e { get; } = Math.E;
#pragma warning restore IDE1006
        public static Scalar Pi { get; } = Math.PI;

        public static Scalar PiHalf { get; } = Math.PI / 2;

        public static Scalar Tau { get; } = Math.PI * 2;

        public static Scalar Sqrt2 { get; } = Math.Sqrt(2);

        public static Scalar GoldenRatio { get; } = 1.618033988749894848204586834;

        public static Scalar IEEE754Epsilon { get; } = __scalar.Epsilon;

        public static Scalar ComputationalEpsilon
        {
            get => _cepsilon;
            set
            {
                if (value * value > IEEE754Epsilon && value < 1)
                    _cepsilon = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(value), $"The computational epislon must be inside the exclusive range ({IEEE754Epsilon.Sqrt():e0}..1).");
            }
        }

        public static Scalar MinValue { get; } = __scalar.MinValue;

        public static Scalar MaxValue { get; } = __scalar.MaxValue;

        public static Scalar NegativeInfinity { get; } = __scalar.NegativeInfinity;

        public static Scalar PositiveInfinity { get; } = __scalar.PositiveInfinity;

        public static Scalar NaN { get; } = __scalar.NaN;

        /// <summary>
        /// The 1x1 zero matrix. This is euqal to the scalar value of 0.
        /// </summary>
        public static Scalar Zero { get; } = new Scalar(0d);

        /// <summary>
        /// The 1x1 identity (unit) matrix. This is euqal to the scalar value of 1.
        /// </summary>
        public static Scalar One { get; } = new Scalar(1d);

        public static Scalar NegativeOne { get; } = new Scalar(-1d);

        public static Scalar Two { get; } = new Scalar(2d);

        /// <summary>
        /// The raw memory size of the <see cref="Scalar"/>-structure in bytes.
        /// </summary>
        public static int BinarySize { get; } = sizeof(Scalar);

        public static ScalarEqualityComparer EqualityComparer { get; } = new ScalarEqualityComparer();

        #endregion
        #region INDEXERS

        public readonly Scalar this[int index] => this[index, 0];

        public readonly Scalar this[int index, Scalar value] => this[index, 0, value];

        public readonly Scalar this[int index, in Scalar value] => this[index, 0, value];

        public readonly Scalar this[int column, int row] => (column, row) == (0, 0) ? this : throw new ArgumentException($"Invalid indices: The type '{typeof(Scalar).FullName}' only holds one value and therefore can only be addressed using the indices (0,0).");

        public readonly Scalar this[int column, int row, Scalar value] => (column, row) == (0, 0) ? value : throw new ArgumentException($"Invalid indices: The type '{typeof(Scalar).FullName}' only holds one value and therefore can only be addressed using the indices (0,0).");

        #endregion
        #region INSTANCE PROPERTIES

        public readonly __scalar Determinant { get; }

        public readonly bool IsNaN => __scalar.IsNaN(Determinant);

        public readonly bool IsNegative => Determinant < 0;

        public readonly bool IsPositive => Determinant > 0;

        public readonly bool IsPositiveDefinite => IsPositive;

        public readonly bool IsFinite => __scalar.IsFinite(Determinant);

        public readonly bool IsInfinity => __scalar.IsInfinity(Determinant);

        public readonly bool IsNegativeInfinity => __scalar.IsNegativeInfinity(Determinant);

        public readonly bool IsPositiveInfinity => __scalar.IsPositiveInfinity(Determinant);

        public readonly Scalar MultiplicativeInverse => Inverse;

        public readonly bool IsInvertible => IsNonZero;

        public readonly bool IsIdentity => Is(One);

        public readonly bool IsOne => Is(One);

        public readonly Scalar AdditiveInverse => Negate();

        public readonly bool IsZero => Is(Zero);

        public readonly bool IsNonZero => !IsZero;

        public readonly bool IsOdd => Decrement().IsEven;

        public readonly bool IsEven => IsMultipleOf(Two);

        public readonly bool IsBinary => IsZero || IsOne;

        public readonly bool IsBetweenZeroAndOne => this >= Zero && this <= One;

        public readonly bool IsInsideUnitSphere => AbsoluteValue.IsBetweenZeroAndOne;

        public readonly Scalar Inverse => new Scalar(1 / Determinant);

        public readonly Scalar AbsoluteValue => Abs();

        public readonly Polynomial CharacteristicPolynomial => new Polynomial(-Determinant, 1);

        public readonly int Sign => CompareTo(Zero);

        public readonly Scalar DecimalPlaces => Subtract(Floor);

        public readonly Scalar Floor => new Scalar(Math.Floor(Determinant));

        public readonly Scalar Ceiling => new Scalar(Math.Ceiling(Determinant));

        public readonly Scalar Rounded => new Scalar(Math.Round(Determinant));

        public readonly bool IsInteger => Is(Floor);

        public readonly bool IsRational => Is(ToFraction(ComputationalEpsilon * ComputationalEpsilon).AsScalar);

        public readonly bool IsPrime => IsPositive && IsInteger && ((bint)Determinant).IsPrime();

        public readonly Scalar[] PrimeFactors => IsInteger && IsPositive ? ((bint)Determinant).PrimeFactorization().Select(b => new Scalar((__scalar)b)).ToArray() : Array.Empty<Scalar>();

        public readonly Scalar Phi => PrimeFactors is { Length: 2 } f ? (__scalar)((f[0] - 1) * (f[1] - 1)) : throw new InvalidOperationException($"φ({this}) is not defined.");

        #endregion
        #region EXPLICIT PROPERTIES

        readonly int IReadonlyNative<Scalar>.BinarySize => BinarySize;

        readonly Scalar Algebra<Scalar>.IMetricVectorSpace<Scalar>.SquaredNorm => Multiply(this);

        readonly Scalar Algebra<Scalar>.IMetricVectorSpace<Scalar>.Normalized => One;

        readonly bool Algebra<Scalar>.IMetricVectorSpace.IsNormalized => IsOne;

        readonly Scalar Algebra<Scalar>.IMetricVectorSpace.Length => Abs();

        readonly Scalar[] Algebra<Scalar>.IComposite1D.Coefficients => new[] { this };

        readonly Scalar[,] Algebra<Scalar>.IComposite2D.Coefficients => new[,] { { this } };

        readonly int Algebra<Scalar>.IComposite1D.Dimension => 1;

        readonly bool Algebra<Scalar>.IComposite.HasNegatives => IsNegative;

        readonly bool Algebra<Scalar>.IComposite.HasPositives => IsPositive;

        readonly bool Algebra<Scalar>.IComposite.HasNaNs => IsNaN;

        readonly Scalar Algebra<Scalar>.IComposite.Sum => this;

        readonly Scalar Algebra<Scalar>.IComposite.Avg => this;

        readonly Scalar Algebra<Scalar>.IComposite.Min => this;

        readonly Scalar Algebra<Scalar>.IComposite.Max => this;

        readonly Scalar Algebra<Scalar>.IMatrix<Scalar>.GaussianReduced => IsZero ? Zero : One;

        readonly Scalar Algebra<Scalar>.IMatrix<Scalar>.OrthonormalBasis => IsZero ? Zero : One;

        readonly Scalar Algebra<Scalar>.IMatrix<Scalar>.Transposed => this;

        readonly Scalar Algebra<Scalar>.IMatrix.Determinant => this;

        readonly Scalar Algebra<Scalar>.IMatrix.Trace => this;

        readonly bool Algebra<Scalar>.IMatrix.IsDiagonal => true;

        readonly bool Algebra<Scalar>.IMatrix.IsUpperTriangular => true;

        readonly bool Algebra<Scalar>.IMatrix.IsLowerTriangular => true;

        readonly bool Algebra<Scalar>.IMatrix.IsSymmetric => true;

        readonly bool Algebra<Scalar>.IMatrix.IsProjection => IsOne || IsZero;

        readonly bool Algebra<Scalar>.IMatrix.IsConferenceMatrix => Is(Multiply(this));

        readonly bool Algebra<Scalar>.IMatrix.IsInvolutory => Multiply(Inverse).IsOne;

        readonly bool Algebra<Scalar>.IMatrix.IsHurwitzStable => IsPositive;

        readonly bool Algebra<Scalar>.IMatrix.IsOrthogonal => IsOne;

        readonly bool Algebra<Scalar>.IMatrix.IsSkewSymmetric => IsZero;

        readonly bool Algebra<Scalar>.IMatrix.IsSignMatrix => IsZero || Abs().IsOne;

        readonly bool Algebra<Scalar>.IMatrix.IsSignatureMatrix => Abs().IsOne;

        readonly Scalar[] Algebra<Scalar>.IMatrix.Eigenvalues => IsZero ? Array.Empty<Scalar>() : new[] { this };

        readonly IEnumerable<Scalar> Algebra<Scalar>.IComposite2D.FlattenedCoefficients => new[] { this };

        readonly (int Columns, int Rows) Algebra<Scalar>.IComposite2D.Dimensions => (1, 1);

        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.MainDiagonal => this;

        readonly Scalar[] Algebra<Scalar>.IMatrix<Scalar, Scalar>.Columns => new[] { this };

        readonly Scalar[] Algebra<Scalar>.IMatrix<Scalar, Scalar>.Rows => new[] { this };

        readonly int Algebra<Scalar>.IMatrix.Rank => IsZero ? 0 : 1;

        readonly Scalar Algebra<Scalar>.IVector<Scalar, Scalar>.HouseholderMatrix => IsZero ? throw new InvalidOperationException("The Householder matrix is undefined for zero vectors.") : Two;

        #endregion
        #region .CTOR / .CCTOR / .DTOR

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scalar(__scalar value) => Determinant = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scalar(params __scalar[] values)
            : this(values[0])
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scalar(IEnumerable<__scalar> values)
            : this(values.ToArray())
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scalar(Scalar scalar) => Determinant = scalar.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Scalar(Scalar* scalar)
            : this(*scalar)
        {
        }

        #endregion
        #region INSTANCE METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Abs() => IsNegative ? Negate() : this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Negate() => new Scalar(-Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Add(Scalar second) => new Scalar(Determinant + second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Add(in Scalar second) => new Scalar(Determinant + second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Add(params Scalar[] others) => others.Aggregate(this, Add);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Subtract(Scalar second) => new Scalar(Determinant - second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Subtract(in Scalar second) => new Scalar(Determinant - second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Subtract(params Scalar[] others) => others.Aggregate(this, Subtract);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Increment() => Add(One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Decrement() => Subtract(One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Dot(in Scalar second) => Multiply(in second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Multiply(Scalar second) => new Scalar(Determinant * second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Multiply(in Scalar second) => new Scalar(Determinant * second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Multiply(params Scalar[] others) => others.Aggregate(this, Multiply);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Divide(Scalar second) => new Scalar(Determinant / second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Divide(in Scalar second) => new Scalar(Determinant / second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar Factor, Scalar Remainder) DivideModulus(in Scalar second) => (Divide(in second), Modulus(in second));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Modulus(in Scalar second) => new Scalar(Determinant % second.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Power(int e) => Power(new Scalar((__scalar)e));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Power(Scalar e) => new Scalar(Math.Pow(Determinant, e.Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Sqrt() => new Scalar(Math.Sqrt(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Exp() => new Scalar(Math.Exp(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Log() => new Scalar(Math.Log(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Sin() => new Scalar(Math.Sin(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Cos() => new Scalar(Math.Cos(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Tan() => new Scalar(Math.Tan(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Cot() => Tan().Inverse;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Complex Cis() => Complex.Cis(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Asin() => new Scalar(Math.Asin(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Acos() => new Scalar(Math.Acos(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Atan() => new Scalar(Math.Atan(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Sinh() => new Scalar(Math.Sinh(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Cosh() => new Scalar(Math.Cosh(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Tanh() => new Scalar(Math.Tanh(Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Coth() => Tanh().Inverse;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Clamp() => Clamp(Zero, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Clamp(Scalar low, Scalar high) => Min(high).Max(low);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Min(Scalar second) => CompareTo(second) <= 0 ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar Max(Scalar second) => CompareTo(second) >= 0 ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsMultipleOf(Scalar other) => Modulus(other).IsZero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsBetween(Scalar min_inclusive, Scalar max_inclusive) => this >= min_inclusive && this <= max_inclusive;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is(Scalar other, Scalar tolerance)
        {
            if ((IsPositiveInfinity && other.IsPositiveInfinity) ||
                (IsNegativeInfinity && other.IsNegativeInfinity) ||
                (IsNaN && other.IsNaN))
                return true;

            return Subtract(other).Abs().CompareTo(tolerance) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is([MaybeNull] Scalar other) => Is(other, ComputationalEpsilon);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsNot([MaybeNull] Scalar other) => !Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Scalar other) => Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? other)
        {
            try
            {
                return Equals((Scalar)other!);
            }
            catch
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => Determinant.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => Determinant.ToString(); // TODO : check if pi or e etc.

        /// <inheritdoc cref="__scalar.ToString(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format) => Determinant.ToString(format);

        /// <inheritdoc cref="__scalar.ToString(IFormatProvider)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(IFormatProvider prov) => Determinant.ToString(prov);

        /// <inheritdoc cref="__scalar.ToString(string,IFormatProvider)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string? format, IFormatProvider? prov) => Determinant.ToString(format, prov);

        public readonly string ToHexString(int digits = 18) => ToString(16, digits);

        public readonly string ToString(int @base, int digits = 18)
        {
            if (IsInteger)
                if (@base == 16)
                    return $"{(ulong)this:X}";

            string chars = "0123456789ABCDEF"[..@base];
            StringBuilder acc = new();
            Scalar place = 1;
            Scalar scalar = this;
            bool dot = false;

            if (!IsFinite)
                return ToString();
            else if (IsNegative)
            {
                acc.Append('-');
                scalar = Abs();
            }

            while (scalar >= @base)
            {
                scalar /= @base;
                place *= @base;
            }

            for (int i = 0; i < digits || place > 1; ++i)
            {
                if (scalar.IsZero && (dot || place < 1))
                    break;
                else if (place < 1 && !dot)
                {
                    acc.Append('.');
                    dot = true;
                }

                acc.Append(chars[(int)scalar]);

                scalar = scalar.DecimalPlaces * @base;
                place /= @base;
            }

            return acc.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToShortString() => ToShortString(null);

        public readonly string ToShortString(string? format)
        {
            if (IsInfinity)
                return "∞";
            else if (IsNegativeInfinity)
                return "-∞";
            else if (IsNaN)
                return "NaN";

            Scalar abs = Abs();
            string s;

            if (abs.Is(Pi))
                s = "π";
            else if (abs.Is(e))
                s = "e";
            else if (abs.Is(Tau))
                s = "τ";
            else if (abs.Divide(Pi) is { IsInteger: true, IsZero: false } f)
                s = $"{f}π";
            else if (abs.Is(GoldenRatio))
                s = "φ";
            else if (abs.IsPositive && abs < 2 * ComputationalEpsilon)
                s = "ε";
            else
                return format is { } ? ToString(format) : ToString();

            return IsNegative ? $"-{s}" : s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(Scalar other) => Determinant.CompareTo(other.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(object? obj) => CompareTo((Scalar)obj!);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly object Clone() => new Scalar(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Fraction ToFraction() => Fraction.FromScalar(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Fraction ToFraction(Scalar accuracy) => Fraction.FromScalar(this, accuracy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar[] ToArray() => new[] { this };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray<T>() where T : unmanaged => ToArray().CopyTo<Scalar, T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToNative<T>(T* dst) where T : unmanaged => ToArray().CopyTo(dst);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Polynomial ToPolynomial() => new Polynomial(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar P, Scalar Q)? DecomposePQ(Scalar phi)
        {
            if (new Polynomial(this, phi - this - 1, 1).Roots.ToArray() is { Length: 2 } arr)
                return (arr[0], arr[1]);

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar U, Scalar D) IwasawaDecompose() => (this, One);

        #endregion
        #region EXPLICIT METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IVector<Scalar, Scalar>.OuterProduct(in Scalar second) => Multiply(in second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IEucledianVectorSpace<Scalar>.AngleTo(in Scalar second) => throw new InvalidOperationException("This operation is undefined for scalar data types.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IComposite<Scalar>.ComponentwiseDivide(in Scalar second) => Multiply(in second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IComposite<Scalar>.ComponentwiseMultiply(in Scalar second) => Divide(in second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMetricVectorSpace<Scalar>.DistanceTo(in Scalar second) => Subtract(in second).Abs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Algebra<Scalar>.IMatrix[] Algebra<Scalar>.IMatrix.GetPrincipalSubmatrices() => global::System.Array.Empty<global::Unknown6656.Mathematics.LinearAlgebra.Algebra<global::Unknown6656.Mathematics.LinearAlgebra.Scalar>.IMatrix>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar>.IVectorSpace<Scalar>.IsLinearDependant(in Scalar other, out Scalar? factor) => (factor = IsZero || other.IsZero ? (Scalar?)null : other / this) != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar>.IEucledianVectorSpace<Scalar>.IsOrthogonal(in Scalar second) => throw new InvalidOperationException("The 'IsOrthogonal' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IVectorSpace<Scalar>.LinearInterpolate(in Scalar other, Scalar factor) => new Scalar((1 - factor) * Determinant + factor * other.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IEucledianVectorSpace<Scalar>.Reflect(in Scalar normal) => throw new InvalidOperationException("The 'reflect' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar>.IEucledianVectorSpace<Scalar>.Refract(in Scalar normal, Scalar eta, out Scalar refracted) => throw new InvalidOperationException("The 'refract' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar>.IMatrix<Scalar, Scalar>.Solve(Scalar v, out Scalar solution)
        {
            solution = NaN;

            if (IsZero)
                return false;
            else
            {
                solution = v / this;

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.GetRow(int row) => this[0, row];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.SetRow(int row, in Scalar vector) => this[0, row, vector];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.GetRows(Range rows) => (this as Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>).GetRegion(0..1, rows);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.SetRows(Range rows, in MatrixNM values) => (this as Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>).SetRegion(0..1, rows, values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.GetColumn(int column) => this[column];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.SetColumn(int column, in Scalar vector) => this[column, vector];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.GetColumns(Range columns) => (this as Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>).GetRegion(columns, 0..1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.SetColumns(Range columns, in MatrixNM values) => (this as Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>).SetRegion(columns, 0..1, values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.GetRegion(Range columns, Range rows)
        {
            if (columns.GetOffsetAndLength(1) is (0, 1))
                if (rows.GetOffsetAndLength(1) is (0, 1))
                    return MatrixNM.FromArray(new Scalar[,] { { this } });
                else
                    throw new ArgumentException("The row indices must only contain the value [0].", nameof(rows));
            else
                throw new ArgumentException("The column indices must only contain the value [0].", nameof(rows));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar, Polynomial>.IMatrix<Scalar, MatrixNM>.SetRegion(Range columns, Range rows, in MatrixNM values)
        {
            if (columns.GetOffsetAndLength(1) is (0, 1))
                if (rows.GetOffsetAndLength(1) is (0, 1))
                    return values.Size == (1, 1) ? values[0, 0] : throw new ArgumentException("The new values must be a matrix with the dimensions (1, 1).", nameof(values));
                else
                    throw new ArgumentException("The row indices must only contain the value [0].", nameof(rows));
            else
                throw new ArgumentException("The column indices must only contain the value [0].", nameof(rows));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.MultiplyRow(int row, Scalar factor) => row == 0 ? Multiply(in factor) : throw new ArgumentException("The row index must be zero.", nameof(row));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.SwapRows(int src_row, int dst_row) => (src_row, dst_row) == (0, 0) ? this : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.AddRows(int src_row, int dst_row) => (this as Algebra<Scalar>.IMatrix<Scalar, Scalar>).AddRows(src_row, dst_row, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.AddRows(int src_row, int dst_row, Scalar factor) =>
            (src_row, dst_row) == (0, 0) ? Determinant * (1 + factor) : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.MultiplyColumn(int col, Scalar factor) => col == 0 ? Multiply(in factor) : throw new ArgumentException("The column index must be zero.", nameof(col));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.SwapColumns(int src_col, int dst_col) => (src_col, dst_col) == (0, 0) ? this : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.AddColumns(int src_col, int dst_col) => (this as Algebra<Scalar>.IMatrix<Scalar, Scalar>).AddColumns(src_col, dst_col, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar Algebra<Scalar>.IMatrix<Scalar, Scalar>.AddColumns(int src_col, int dst_col, Scalar factor) =>
            (src_col, dst_col) == (0, 0) ? Determinant * (1 + factor) : throw new ArgumentException("The source and destination columns must have the index zero.");

        #endregion
        #region STATIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(Scalar s1, Scalar s2, Scalar? error = null) => s1.Is(s2, error ?? ComputationalEpsilon);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Random() => Numerics.Random.XorShift.NextScalar();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Negate(Scalar s) => s.Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Abs(Scalar s) => s.Abs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Sqrt(Scalar s) => s.Sqrt();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Exp(Scalar s) => s.Exp();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Log(Scalar s) => s.Log();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Log(Scalar s, Scalar basis) => new Scalar(Math.Log(s.Determinant, basis.Determinant));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Sin(Scalar s) => s.Sin();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Cos(Scalar s) => s.Cos();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Tan(Scalar s) => s.Tan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex Cis(Scalar s) => s.Cis();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Asin(Scalar s) => s.Asin();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Acos(Scalar s) => s.Acos();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Atan(Scalar s) => s.Atan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Sinh(Scalar s) => s.Sinh();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Cosh(Scalar s) => s.Cosh();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Tanh(Scalar s) => s.Tanh();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Add(Scalar s1, Scalar s2) => s1.Add(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Subtract(Scalar s1, Scalar s2) => s1.Subtract(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Multiply(Scalar s1, Scalar s2) => s1.Multiply(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Divide(Scalar s1, Scalar s2) => s1.Divide(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar Modulus(Scalar s1, Scalar s2) => s1.Modulus(s2);

        public static bool TryParse(string str, out Scalar scalar)
        {
            bool success = true;

            str = str.Remove("_").ToLowerInvariant().Trim();
            scalar = Zero;

            if (str.Match(REGEX_BIN, out ReadOnlyIndexer<string, string>? groups))
                scalar = Convert.ToUInt64(groups["num"].Remove("0b").Remove("b"), 2);
            else if (str.Match(REGEX_OCT, out groups))
                scalar = Convert.ToUInt64(groups["num"].Remove("0o").Remove("o"), 8);
            else if (str.Match(REGEX_HEX, out groups))
                scalar = Convert.ToUInt64(groups["num"].Remove("0x").Remove("x"), 16);
            else if (str.Match(REGEX_DEC1, out groups) || str.Match(REGEX_DEC2, out groups))
            {
                string @const = groups["const"];
                string factor = groups["factor"];
                Scalar f = One;

                if (!string.IsNullOrEmpty(factor))
                    f = (Scalar)decimal.Parse(factor, NumberStyles.Any);

                if (string.IsNullOrEmpty(@const))
                    scalar = string.IsNullOrEmpty(factor) ? Zero : f;
                else
                    scalar = f * (@const switch
                    {
                        "" => One,
                        "e" => e,
                        "pi" or "π" => Pi,
                        "tau" or "τ" => Tau,
                        "phi" or "φ" => GoldenRatio,
                    });
            }
            else
                success = false;

            if (str.StartsWith("-"))
                scalar = scalar.Negate();

            return success;
        }

        #endregion
        #region OPERATORS

        /// <summary>
        /// Compares whether the two given scalars are equal regarding their coefficients.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Scalar m1, Scalar m2) => m1.Is(m2);

        /// <summary>
        /// Compares whether the two given scalars are unequal regarding their coefficients.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Scalar m1, Scalar m2) => m1.IsNot(m2);

        /// <summary>
        /// Compares whether the first given scalar is smaller than the second one.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Scalar m1, Scalar m2) => m1.CompareTo(m2) < 0;

        /// <summary>
        /// Compares whether the first given scalar is smaller than or equal to the second one.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Scalar m1, Scalar m2) => m1.CompareTo(m2) <= 0;

        /// <summary>
        /// Compares whether the first given scalar is greater than the second one.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Scalar m1, Scalar m2) => m1.CompareTo(m2) > 0;

        /// <summary>
        /// Compares whether the first given scalar is greater than or equal to the second one.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Comparison result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Scalar m1, Scalar m2) => m1.CompareTo(m2) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator ++(Scalar s) => s.Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator --(Scalar s) => s.Decrement();

        /// <summary>
        /// Identity function (returns the given scalar unchanged)
        /// </summary>
        /// <param name="m">Original scalar</param>
        /// <returns>Unchanged scalar</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator +(Scalar m) => m;

        /// <summary>
        /// Negates the given scalar
        /// </summary>
        /// <param name="m">Original scalar</param>
        /// <returns>Negated scalar</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator -(Scalar m) => m.Negate();

        /// <summary>
        /// Performs the subtraction of two scalars by subtracting their respective coefficients.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Subtraction result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator -(Scalar m1, Scalar m2) => m1.Subtract(m2);

        /// <summary>
        /// Performs the addition of two scalars by adding their respective coefficients.
        /// </summary>
        /// <param name="m1">First scalar</param>
        /// <param name="m2">Second scalar</param>
        /// <returns>Addition result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator +(Scalar m1, Scalar m2) => m1.Add(m2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator *(Scalar m, Scalar v) => m.Multiply(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator ^(Scalar b, Scalar e) => b.Power(e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator ^(Scalar b, int e) => b.Power(e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator /(Scalar m, Scalar f) => m.Divide(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar operator %(Scalar m, Scalar f) => m.Modulus(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator decimal(Scalar m) => (decimal)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(MatrixNM m) => m[0, 0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(VectorN m) => m[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(string str) => TryParse(str, out Scalar scalar) ? scalar : Zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bint(Scalar m) => new bint(m.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(byte t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(sbyte t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(short t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(ushort t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(int t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(uint t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(long t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(ulong t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(float t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(double t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(decimal t) => new Scalar((__scalar)t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(Scalar m) => (double)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(Scalar m) => (float)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ulong(Scalar m) => (ulong)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(Scalar m) => (long)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(Scalar m) => (uint)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(Scalar m) => (int)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator ushort(Scalar m) => (ushort)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator short(Scalar m) => (short)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator byte(Scalar m) => (byte)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator sbyte(Scalar m) => (sbyte)m.Determinant;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar(Scalar<__scalar> t) => new Scalar(t.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar<__scalar>(Scalar m) => new Scalar<__scalar>(m.Determinant);

        #endregion

        /*

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToNative<T>(T* dst)
            where T : unmanaged
        {
            byte* pdst = (byte*)dst;

            fixed (Scalar* ptr = &this)
                for (int i = 0; i < BinarySize; ++i)
                    pdst[i] = *((byte*)ptr + i);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromNative<T>(T* src)
            where T : unmanaged
        {
            byte* psrc = (byte*)src;

            fixed (Scalar* ptr = &this)
                for (int i = 0; i < BinarySize; ++i)
                    *((byte*)ptr + i) = psrc[i];
        }

        */
    }

    [StructLayout(LayoutKind.Sequential), NativeCppClass, Serializable, CLSCompliant(false)]
    public unsafe readonly /* ref */ partial struct Scalar<T>
        : IScalar<Scalar<T>>
        , Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>
        , Algebra<Scalar<T>>.IVector<Scalar<T>, Scalar<T>>
        , Algebra<Scalar<T>, Polynomial<T>>.IComposite1D
        , Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>
        , IComparable<Scalar<T>>
        , IEquatable<Scalar<T>>
        , IComparable
        , ICloneable
        where T : unmanaged, IComparable<T>
    {
        #region STATIC FIELDS

        private static readonly Dictionary<OpType, MethodInfo> _operators = new Dictionary<OpType, MethodInfo>();
        private static readonly T _zero;
        private static readonly T _one;

        #endregion
        #region STATIC PROPERTIES

        public static Scalar<T> Zero { get; }

        public static Scalar<T> One { get; }

        public static Scalar<T> NegativeOne { get; }

        public static Scalar<T> Two { get; }

        public static ScalarEqualityComparer<T> EqualityComparer { get; } = new ScalarEqualityComparer<T>();

        #endregion
        #region INDEXERS

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite1D.this[int index] => index is 0 ? this : throw new ArgumentOutOfRangeException("The index must be zero.");

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite1D<Scalar<T>>.this[int index, Scalar<T> value] => index is 0 ? value : throw new ArgumentOutOfRangeException("The index must be zero.");

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.this[int column, in Scalar<T> vector] => column is 0 ? vector : throw new ArgumentOutOfRangeException("The index must be zero.");

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite2D.this[int column, int row] => (column, row) is (0, 0) ? this : throw new ArgumentOutOfRangeException("The indices must be (0, 0).");

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.this[int column] => column is 0 ? this : throw new ArgumentOutOfRangeException("The index must be zero.");

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite2D<Scalar<T>>.this[int column, int row, Scalar<T> value] => (column, row) is (0, 0) ? value : throw new ArgumentOutOfRangeException("The indices must be (0, 0).");

        #endregion
        #region INSTANCE PROPERTIES

        public readonly T Value { get; }

        public readonly bool IsNaN => MathFunction(__scalar.IsNaN);

        public readonly bool IsNegative => this < Zero;

        public readonly bool IsPositive => this > Zero;

        public readonly bool IsPositiveDefinite => IsPositive;

        public readonly bool IsNegativeInfinity => MathFunction(__scalar.IsNegativeInfinity);

        public readonly bool IsPositiveInfinity => MathFunction(__scalar.IsPositiveInfinity);

        public readonly bool IsInfinity => MathFunction(__scalar.IsInfinity);

        public readonly bool IsFinite => MathFunction(__scalar.IsFinite);

        public readonly Scalar<T> Inverse => One.Divide(this);

        [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
        public readonly Scalar<T> MultiplicativeInverse => Inverse;

        public readonly bool IsInvertible => IsNonZero;

        public readonly bool IsOne => Equals(Value, _one);

        public readonly Scalar<T> AdditiveInverse => Negate();

        public readonly bool IsZero => Equals(Value, _zero);

        public readonly bool IsNonZero => !IsZero;

        public readonly bool IsBinary => IsZero || IsOne;

        public readonly int BinarySize => sizeof(T);

        public readonly bool IsNormalized => IsOne;

        public readonly bool IsInsideUnitSphere => Abs().IsBetweenZeroAndOne;

        public readonly bool IsBetweenZeroAndOne => this >= Zero && this <= One;

        public readonly int Sign => CompareTo(Zero);

        public readonly Scalar<T> DecimalPlaces => Modulus(One);

        public readonly Scalar<T> Ceiling => IsInteger ? Floor : Floor.Increment();

        public readonly Scalar<T> Floor => Subtract(DecimalPlaces);

        public readonly Scalar<T> Rounded => DecimalPlaces.Multiply(Two) < One ? Floor : Ceiling;

        public readonly bool IsInteger => Is(Rounded);

        public readonly bool IsPrime => IsPositive && IsInteger && ((bint)(dynamic)Value).IsPrime();

        public readonly Scalar<T>[] PrimeFactors => IsInteger && IsPositive ? ((bint)(dynamic)Value).PrimeFactorization().Select(b => new Scalar<T>((T)(dynamic)b)).ToArray() : Array.Empty<Scalar<T>>();

        public readonly Scalar<T> Phi => PrimeFactors is { Length: 2 } f ? f[0].Decrement().Multiply(f[1].Decrement()) : throw new InvalidOperationException($"φ({this}) is not defined.");

        #endregion
        #region EXPLICIT PROPERTIES

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>>.GaussianReduced => IsZero ? Zero : One;

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>>.OrthonormalBasis => IsZero ? Zero : One;

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>>.Transposed => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix.Determinant => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix.Trace => this;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsDiagonal => true;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsUpperTriangular => true;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsLowerTriangular => true;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsSymmetric => true;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsProjection => IsOne || IsZero;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsConferenceMatrix => Is(Multiply(this));

        readonly bool Algebra<Scalar<T>>.IMatrix.IsInvolutory => Multiply(Inverse).IsOne;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsHurwitzStable => IsPositive;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsOrthogonal => IsOne;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsSkewSymmetric => IsZero;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsSignMatrix => IsZero || Abs().IsOne;

        readonly bool Algebra<Scalar<T>>.IMatrix.IsSignatureMatrix => Abs().IsOne;

        readonly Polynomial<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix.CharacteristicPolynomial => new Polynomial<T>(this, NegativeOne);

        readonly Scalar<T>[] Algebra<Scalar<T>>.IMatrix.Eigenvalues => IsZero ? Array.Empty<Scalar<T>>() : new[] { this };

        readonly int Algebra<Scalar<T>>.IMatrix.Rank => IsZero ? 0 : 1;

        readonly Scalar<T>[,] Algebra<Scalar<T>>.IComposite2D.Coefficients => new[,] { { this } };

        readonly IEnumerable<Scalar<T>> Algebra<Scalar<T>>.IComposite2D.FlattenedCoefficients => new[] { this };

        readonly (int Columns, int Rows) Algebra<Scalar<T>>.IComposite2D.Dimensions => (1, 1);

        readonly bool Algebra<Scalar<T>>.IComposite.HasNegatives => IsNegative;

        readonly bool Algebra<Scalar<T>>.IComposite.HasPositives => IsPositive;

        readonly bool Algebra<Scalar<T>>.IComposite.HasNaNs => IsNaN;

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite.Sum => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite.Avg => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite.Min => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IComposite.Max => this;

        readonly Scalar<T> Algebra<Scalar<T>>.IMetricVectorSpace<Scalar<T>>.SquaredNorm => Multiply(this);

        readonly Scalar<T> Algebra<Scalar<T>>.IMetricVectorSpace<Scalar<T>>.Normalized => One;

        readonly Scalar<T> Algebra<Scalar<T>>.IMetricVectorSpace.Length => this;

        readonly Scalar<T>[] Algebra<Scalar<T>>.IComposite1D.Coefficients => new[] { this };

        readonly int Algebra<Scalar<T>>.IComposite1D.Dimension => 1;

        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.MainDiagonal => throw new NotImplementedException();

        readonly Scalar<T>[] Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.Columns => throw new NotImplementedException();

        readonly Scalar<T>[] Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.Rows => throw new NotImplementedException();

        readonly Scalar<T> Algebra<Scalar<T>>.IVector<Scalar<T>, Scalar<T>>.HouseholderMatrix => throw new NotImplementedException();

        #endregion
        #region .CTOR / .CCTOR / .DTOR

        static Scalar()
        {
            Type T = typeof(T);
            OpType[] critical =
            {
                OpType.op_Equality,
                OpType.op_Addition,
                OpType.op_UnaryNegation,
                OpType.op_Division,
                OpType.op_Multiply,
                OpType.op_Modulus,
            };

            _zero = default;
            _one = FunctionExtensions.TryAll<T>(
                () => (dynamic)_zero + 1,
                () =>
                {
                    dynamic _1 = _zero;

                    return _1++;
                },
                () => (T)typeof(T).GetMethod("Increment", BindingFlags.Instance | BindingFlags.Public)!.Invoke(_zero, Array.Empty<object>())!,
                () => (dynamic)_zero + (T)(dynamic)1
            ); // TODO : fix this shit!

            foreach (OpType op in Enum.GetValues(typeof(OpType)))
                try
                {
                    _operators[op] = T.GetMethod(op.ToString(), BindingFlags.Static | BindingFlags.Public)!;
                }
                catch (AmbiguousMatchException)
                {
                    Type tref = typeof(T).MakeByRefType();
                    _operators[op] = (from m in T.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                      where m.Name == op.ToString()
                                      let pars = m.GetParameters()
                                      let pt0 = pars[0].ParameterType
                                      let pt1 = pars[1].ParameterType
                                      where pt0.IsAssignableFrom(typeof(T)) || pt0.IsAssignableFrom(tref)
                                      where pt1.IsAssignableFrom(typeof(T)) || pt1.IsAssignableFrom(tref)
                                      select m).FirstOrDefault();
                }
                catch
                {
                }
                finally
                {
                    if (!_operators.TryGetValue(op, out MethodInfo? m) || m is null)
                        _operators[op] = (op switch
                        {
                            _ when critical.Contains(op) => throw new InvalidOperationException($"The type '{T}' canot be useed as generic parameter for the type '{typeof(Scalar<>)}', as it does not implement the operator '{op}'."),
                            OpType.op_Inequality => (Delegate)new Func<object, T, T, bool>((_, t1, t2) => !OP<bool>(OpType.op_Equality, t1, t2)),
                            OpType.op_GreaterThan => new Func<object, T, T, bool>((_, t1, t2) => t1.CompareTo(t2) > 0),
                            OpType.op_LessThan => new Func<object, T, T, bool>((_, t1, t2) => t1.CompareTo(t2) < 0),
                            OpType.op_GreaterThanOrEqual => new Func<object, T, T, bool>((_, t1, t2) => t1.CompareTo(t2) >= 0),
                            OpType.op_LessThanOrEqual => new Func<object, T, T, bool>((_, t1, t2) => t1.CompareTo(t2) <= 0),
                            OpType.op_Subtraction => new Func<object, T, T, T>((_, t1, t2) => OP<T>(OpType.op_Addition, t1, OP(OpType.op_UnaryNegation, t2))),
                            OpType.op_UnaryPlus => new Func<object, T, T>((_, t) => t),
                            OpType.op_Increment => new Func<object, T, T, T>((_, t1, t2) => OP<T>(OpType.op_Addition, t1, _one)),
                            OpType.op_Decrement => new Func<object, T, T, T>((_, t1, t2) => OP<T>(OpType.op_Addition, t1, OP(OpType.op_UnaryNegation, _one))),
                            _ => throw new InvalidProgramException(),
                        }).Method;
                }

            foreach (MethodInfo nfo in _operators.Values)
                RuntimeHelpers.PrepareMethod(nfo.MethodHandle);

            Zero = new Scalar<T>(_zero);
            One = new Scalar<T>(_one);
            Two = One.Add(One);
            NegativeOne = One.Negate();
        }

        public Scalar(T* ptr)
            : this(*ptr)
        {
        }

        public Scalar(T value) => Value = value;

        public Scalar(Scalar<T>* ptr)
            : this(*ptr)
        {
        }

        public Scalar(Scalar<T> scalar)
            : this(scalar.Value)
        {
        }

        #endregion
        #region INSTANCE METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Negate() => OP(OpType.op_UnaryNegation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Add(in Scalar<T> second) => OP(OpType.op_Addition, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Add(params Scalar<T>[] others) => others.Aggregate(this, Add);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Subtract(in Scalar<T> second) => OP(OpType.op_Subtraction, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Subtract(params Scalar<T>[] others) => others.Aggregate(this, Subtract);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Increment() => Add(One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Decrement() => Subtract(One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Dot(in Scalar<T> second) => Multiply(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Multiply(in Scalar<T> second) => OP(OpType.op_Multiply, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Multiply(params Scalar<T>[] others) => others.Aggregate(this, Multiply);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Divide(in Scalar<T> second) => OP(OpType.op_Division, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar<T> Factor, Scalar<T> Remainder) DivideModulus(in Scalar<T> second) => (Divide(in second), Modulus(in second));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Modulus(in Scalar<T> second) => OP(OpType.op_Modulus, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Multiply(Scalar<T> second) => OP(OpType.op_Multiply, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Divide(Scalar<T> second) => OP(OpType.op_Division, second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Power(int e)
        {
            if (e == 0)
                return One;
            else if (e < 0)
                return Power(-e).Inverse;

            Scalar<T> r = One;
            Scalar<T> p = this;

            while (e > 0)
                if ((e & 1) == 1)
                {
                    --e;
                    r = r.Multiply(p);
                }
                else
                {
                    e /= 2;
                    p = p.Multiply(p);
                }

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Clamp() => Clamp(Zero, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Clamp(Scalar<T> low, Scalar<T> high) => Max(low).Min(high);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar<T> P, Scalar<T> Q)? DecomposePQ(Scalar<T> phi) =>
            new Polynomial<T>(this, phi.Subtract(Increment()), One).Roots.ToArray() is { Length: 2 } arr ? ((Scalar<T>, Scalar<T>)?)(arr[0], arr[1]) : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly (Scalar<T> U, Scalar<T> D) IwasawaDecompose() => (this, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Min(Scalar<T> second) => this <= second ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Max(Scalar<T> second) => this >= second ? this : second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Abs() => IsNegative ? Negate() : this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Sqrt() => MathFunction(Math.Sqrt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Exp() => MathFunction(Math.Exp);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Log() => MathFunction(Math.Log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Sin() => MathFunction(Math.Sin);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Cos() => MathFunction(Math.Cos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Tan() => MathFunction(Math.Tan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Asin() => MathFunction(Math.Asin);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Acos() => MathFunction(Math.Acos);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Atan() => MathFunction(Math.Atan);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Sinh() => MathFunction(Math.Sinh);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Cosh() => MathFunction(Math.Cosh);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T> Tanh() => MathFunction(Math.Tanh);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is(Scalar<T> o, Scalar<T> tolerance) => Subtract(o).Abs() <= tolerance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Is(Scalar<T> o) => Equals(Value, o.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsNot(Scalar<T> o) => !Is(o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? other)
        {
            if (other is { })
                try
                {
                    return Equals((Scalar<T>)other);
                }
                catch
                {
                    try
                    {
                        return Equals((T)other);
                    }
                    catch
                    {
                    }
                }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Scalar<T> other) => Is(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(object? other) => other is Scalar<T> s ? CompareTo(s)
                                                     : other is T v ? CompareTo(new Scalar<T>(v)) : throw new ArgumentException($"A value of the type '{other?.GetType()}' cannot be compared to an instance of '{typeof(Scalar<T>)}'.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(Scalar<T> other) => Value.CompareTo(other.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => Value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => Value.ToString()!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly object Clone() => new Scalar<T>(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Scalar<T>[] ToArray() => new[] { this };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Polynomial<T> ToPolynomial() => new Polynomial<T>(this);

        #endregion
        #region EXPLICIT METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IVector<Scalar<T>, Scalar<T>>.OuterProduct(in Scalar<T> second) => Multiply(in second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Algebra<Scalar<T>>.IMatrix[] Algebra<Scalar<T>>.IMatrix.GetPrincipalSubmatrices() => Array.Empty<Algebra<Scalar<T>>.IMatrix>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar<T>>.IVectorSpace<Scalar<T>>.IsLinearDependant(in Scalar<T> other, out Scalar<T>? factor) => (factor = IsNonZero && other.IsNonZero ? (Scalar<T>?)Divide(other) : null) != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IVectorSpace<Scalar<T>>.LinearInterpolate(in Scalar<T> other, Scalar<T> factor) => Multiply(One.Subtract(factor)).Add(other.Multiply(factor));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMetricVectorSpace<Scalar<T>>.DistanceTo(in Scalar<T> second) => Subtract(second).Abs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IEucledianVectorSpace<Scalar<T>>.AngleTo(in Scalar<T> second) => throw new InvalidOperationException("This operation is undefined for scalar data types.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar<T>>.IEucledianVectorSpace<Scalar<T>>.IsOrthogonal(in Scalar<T> second) => throw new InvalidOperationException("The 'IsOrthogonal' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IComposite<Scalar<T>>.ComponentwiseDivide(in Scalar<T> second) => Divide(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IComposite<Scalar<T>>.ComponentwiseMultiply(in Scalar<T> second) => Multiply(second);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IEucledianVectorSpace<Scalar<T>>.Reflect(in Scalar<T> normal) => throw new InvalidOperationException("The 'reflect' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar<T>>.IEucledianVectorSpace<Scalar<T>>.Refract(in Scalar<T> normal, Scalar<T> eta, out Scalar<T> refracted) => throw new InvalidOperationException("The 'refract' method is undefined for scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly bool Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.Solve(Scalar<T> v, out Scalar<T> solution) => throw new InvalidOperationException("The 'solve' method is undefined for numeric scalar values.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.GetRow(int row) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>)[0, row];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.SetRow(int row, in Scalar<T> vector) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>)[0, row, vector];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.GetRows(Range rows) =>
            (this as Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>).GetRegion(0..1, rows);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.SetRows(Range rows, in MatrixNM<T> values) =>
            (this as Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>).SetRegion(0..1, rows, values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.GetColumn(int column) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>)[column];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.SetColumn(int column, in Scalar<T> vector) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>)[column, vector];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.GetColumns(Range columns) =>
            (this as Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>).GetRegion(columns, 0..1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.SetColumns(Range columns, in MatrixNM<T> values) =>
            (this as Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>).SetRegion(columns, 0..1, values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly MatrixNM<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.GetRegion(Range columns, Range rows)
        {
            if (columns.GetOffsetAndLength(1) is (0, 1))
                if (rows.GetOffsetAndLength(1) is (0, 1))
                    return MatrixNM<T>.FromArray(new Scalar<T>[,] { { this } });
                else
                    throw new ArgumentException("The row indices must only contain the value [0].", nameof(rows));
            else
                throw new ArgumentException("The column indices must only contain the value [0].", nameof(rows));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>, Polynomial<T>>.IMatrix<Scalar<T>, MatrixNM<T>>.SetRegion(Range columns, Range rows, in MatrixNM<T> values)
        {
            if (columns.GetOffsetAndLength(1) is (0, 1))
                if (rows.GetOffsetAndLength(1) is (0, 1))
                    return values.Size == (1, 1) ? (Scalar<T>)values[0, 0] : throw new ArgumentException("The new values must be a matrix with the dimensions (1, 1).", nameof(values));
                else
                    throw new ArgumentException("The row indices must only contain the value [0].", nameof(rows));
            else
                throw new ArgumentException("The column indices must only contain the value [0].", nameof(rows));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.MultiplyRow(int row, Scalar<T> factor) => row == 0 ? Multiply(in factor) : throw new ArgumentException("The row index must be zero.", nameof(row));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.SwapRows(int src_row, int dst_row) => (src_row, dst_row) == (0, 0) ? this : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.AddRows(int src_row, int dst_row) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>).AddRows(src_row, dst_row, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.AddRows(int src_row, int dst_row, Scalar<T> factor) =>
            (src_row, dst_row) == (0, 0) ? Multiply(factor.Increment()) : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.MultiplyColumn(int col, Scalar<T> factor) => col == 0 ? Multiply(in factor) : throw new ArgumentException("The column index must be zero.", nameof(col));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.SwapColumns(int src_col, int dst_col) => (src_col, dst_col) == (0, 0) ? this : throw new ArgumentException("The source and destination rows must have the index zero.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.AddColumns(int src_col, int dst_col) => (this as Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>).AddColumns(src_col, dst_col, One);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Scalar<T> Algebra<Scalar<T>>.IMatrix<Scalar<T>, Scalar<T>>.AddColumns(int src_col, int dst_col, Scalar<T> factor) =>
            (src_col, dst_col) == (0, 0) ? Multiply( factor.Increment()) : throw new ArgumentException("The source and destination columns must have the index zero.");

        #endregion
        #region PRIVATE METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MathFunction(Func<__scalar, bool> func) => func((__scalar)(dynamic)Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Scalar<T> MathFunction(Func<__scalar, __scalar> func) => new Scalar<T>((T)(dynamic)(__scalar)func((__scalar)(dynamic)Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Scalar<T> OP(OpType op) => new Scalar<T>(OP(op, Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Scalar<T> OP(OpType op, Scalar<T> t) => new Scalar<T>(OP<T>(op, Value, t.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private U OP<U>(OpType op, Scalar<T> t) => OP<U>(op, Value, t.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T OP(OpType op, T t) => (T)_operators[op].Invoke(null, new object[] { t });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static U OP<U>(OpType op, T t1, T t2) => (U)_operators[op].Invoke(null, new object[] { t1, t2 });

        #endregion
        #region STATIC METHODS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is(Scalar<T> s1, Scalar<T> s2, Scalar<T>? error = null) => error is null ? s1.Is(s2) : s1.Is(s2, error.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Negate(Scalar<T> s) => s.Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Abs(Scalar<T> s) => s.Abs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Sqrt(Scalar<T> s) => s.Sqrt();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Add(Scalar<T> s1, Scalar<T> s2) => s1.Add(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Subtract(Scalar<T> s1, Scalar<T> s2) => s1.Subtract(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Multiply(Scalar<T> s1, Scalar<T> s2) => s1.Multiply(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Divide(Scalar<T> s1, Scalar<T> s2) => s1.Divide(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> Modulus(Scalar<T> s1, Scalar<T> s2) => s1.Modulus(s2);

        // TODO : parse

        #endregion
        #region OPERATORS

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Scalar<T> s1, Scalar<T> s2) => s1.Is(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Scalar<T> s1, Scalar<T> s2) => !(s1 == s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Scalar<T> s1, Scalar<T> s2) => OP<bool>(OpType.op_LessThan, s1, s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Scalar<T> s1, Scalar<T> s2) => OP<bool>(OpType.op_GreaterThan, s1, s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Scalar<T> s1, Scalar<T> s2) => OP<bool>(OpType.op_LessThanOrEqual, s1, s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Scalar<T> s1, Scalar<T> s2) => OP<bool>(OpType.op_GreaterThanOrEqual, s1, s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator +(Scalar<T> s) => s;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator -(Scalar<T> s) => s.Negate();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator ++(Scalar<T> s) => s.Increment();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator --(Scalar<T> s) => s.Decrement();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator +(Scalar<T> s1, Scalar<T> s2) => s2.Add(s1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator -(Scalar<T> s1, Scalar<T> s2) => s2.Subtract(s1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator *(Scalar<T> s1, Scalar<T> s2) => s1.Multiply(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator ^(Scalar<T> s, int c) => s.Power(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Scalar<T> operator /(Scalar<T> s1, Scalar<T> s2) => s1.Divide(s2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar<T>(T v) => new Scalar<T>(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar<T>(T* ptr) => new Scalar<T>(ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Scalar<T>(Scalar<T>* ptr) => new Scalar<T>(ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(Scalar<T> s) => s.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar<T>(Scalar s) => new Scalar<T>((T)(dynamic)s.Determinant);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Scalar(Scalar<T> s) => (__scalar)(dynamic)s.Value;

        #endregion
        #region NESTED TYPES

        private enum OpType
        {
            op_Equality,
            op_Inequality,
            op_GreaterThan,
            op_LessThan,
            op_GreaterThanOrEqual,
            op_LessThanOrEqual,
            op_Addition,
            op_Subtraction,
            op_Division,
            op_Modulus,
            op_Multiply,
            op_UnaryNegation,
            op_UnaryPlus,
            op_Increment,
            op_Decrement,
        }

        #endregion
    }

    /// <summary>
    /// The equality comparer to be used with the scalar type <see cref="Scalar"/>.
    /// </summary>
    public sealed class ScalarEqualityComparer
        : IEqualityComparer<Scalar>
        , IEqualityComparer<double>
        , IEqualityComparer<float>
    {
        /// <inheritdoc cref="Scalar.Equals(Scalar)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Scalar x, Scalar y) => x.Is(y);

        /// <inheritdoc cref="Scalar.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Scalar obj) => obj.GetHashCode();

        /// <inheritdoc cref="double.Equals(double)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(double x, double y) => Scalar.Is(x, y);

        /// <inheritdoc cref="double.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(double obj) => obj.GetHashCode();

        /// <inheritdoc cref="float.Equals(float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(float x, float y) => Scalar.Is(x, y);

        /// <inheritdoc cref="float.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(float obj) => obj.GetHashCode();
    }

    /// <summary>
    /// The equality comparer to be used with the scalar type <see cref="Scalar{T}"/>.
    /// </summary>
    public sealed class ScalarEqualityComparer<T>
        : IEqualityComparer<Scalar<T>>
        , IEqualityComparer<T>
        where T : unmanaged, IComparable<T>
    {
        /// <inheritdoc cref="Scalar{T}.Equals(Scalar{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Scalar<T> x, Scalar<T> y) => x.Is(y);

        /// <inheritdoc cref="Scalar{T}.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Scalar<T> obj) => obj.GetHashCode();

        /// <inheritdoc cref="T.Equals(T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T x, T y) => x.Equals(y) || x.CompareTo(y) == 0;

        /// <inheritdoc cref="T.GetHashCode"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(T obj) => obj.GetHashCode();
    }
}
