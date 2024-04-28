using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Data;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.LinearAlgebra;


[Serializable]
public sealed unsafe class CompressedStorageFormat<Field>
    where Field : unmanaged, IField<Field>
{
    public (int Columns, int Rows) Dimensions { get; }
    public Field[] Values { get; }
    public int[] Rows { get; }
    public int[] Cols { get; }


    public int CompressedSize => sizeof(Field) * Values.Length + sizeof(int) * (Rows.Length + Cols.Length);

    public int UncompressedSize => sizeof(Field) * Dimensions.Rows * Dimensions.Columns;

    public double CompressionEfficency => 1.0 - ((double)CompressedSize / UncompressedSize);


    public CompressedStorageFormat(Algebra<Field>.IComposite2D matrix)
        : this(matrix.Coefficients)
    {
    }

    public CompressedStorageFormat(Field[,] matrix)
    {
        Dimensions = (matrix.GetLength(0), matrix.GetLength(1));

        List<Field> vals = [];
        List<int> rows = [];
        List<int> cols = [];

        for (int c = 0; c < Dimensions.Columns; ++c)
        {
            for (int r = 0; r < Dimensions.Rows; ++r)
                if (matrix[c, r] is { IsNonZero: true } v)
                {
                    vals.Add(v);
                    rows.Add(r);
                }

            cols.Add(vals.Count);
        }

        Values = [.. vals];
        Rows = [.. rows];
        Cols = [.. cols];
    }

    private CompressedStorageFormat(byte[] bytes)
    {
        fixed (byte* ptr = bytes)
        {
            int i = 0;
            int* iptr = (int*)ptr;
            Field* sptr = (Field*)(iptr + 5);

            int c = iptr[i++];
            int r = iptr[i++];
            Field[] vs = new Field[iptr[i++]];
            int[] rs = new int[iptr[i++]];
            int[] cs = new int[iptr[i++]];

            for (i = 0; i < vs.Length; ++i)
                vs[i] = sptr[i];

            iptr = (int*)(sptr + vs.Length);

            for (i = 0; i < rs.Length; ++i)
                rs[i] = iptr[i];

            for (; i - rs.Length < cs.Length; ++i)
                cs[i - rs.Length] = iptr[i];

            Dimensions = (c, r);
            Values = vs;
            Rows = rs;
            Cols = cs;
        }
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[sizeof(int) * (5 + Rows.Length + Cols.Length) + sizeof(Field) * Values.Length];

        fixed (byte* ptr = bytes)
        {
            int i = 0;
            int* iptr = (int*)ptr;
            Field* sptr = (Field*)(iptr + 5);

            iptr[i++] = Dimensions.Columns;
            iptr[i++] = Dimensions.Rows;
            iptr[i++] = Values.Length;
            iptr[i++] = Rows.Length;
            iptr[i++] = Cols.Length;

            for (i = 0; i < Values.Length; ++i)
                sptr[i] = Values[i];

            iptr = (int*)(sptr + Values.Length);

            for (i = 0; i < Rows.Length; ++i)
                iptr[i] = Rows[i];

            for (; i - Rows.Length < Cols.Length; ++i)
                iptr[i] = Cols[i - Rows.Length];
        }

        return bytes;
    }

    public Field[,] ToMatrix()
    {
        Field[,] mat = new Field[Dimensions.Columns, Dimensions.Rows];
        List<int> cols = [.. Cols];

        for (int i = 0, c = 0; i < Values.Length; ++i)
        {
            while (i >= cols[0])
            {
                cols.RemoveAt(0);
                ++c;
            }

            mat[c, Rows[i]] = Values[i];
        }

        return mat;
    }

    public static CompressedStorageFormat<Field> FromBytes(byte[] bytes) => new(bytes);

    public static CompressedStorageFormat<Field> FromMatrix(Field[,] matrix) => new(matrix);

    public static CompressedStorageFormat<Field> FromMatrix<T>(T matrix) where T : Algebra<Field>.IComposite2D => new(matrix);

    public static implicit operator byte[](CompressedStorageFormat<Field> compressed) => compressed.ToBytes();

    public static implicit operator CompressedStorageFormat<Field>(byte[] bytes) => FromBytes(bytes);
}

public interface IVectorN<Vector, Scalar>
    : IEnumerable<Scalar>
    , IComparable<Vector>
    , IComparable
    , ICloneable
    where Vector : IVectorN<Vector, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    static abstract Vector FromCoefficients(params Scalar[] coefficients);

    static abstract Vector FromCoefficients(IEnumerable<Scalar> coefficients);
}

public interface IMatrixNM<Vector, Matrix, Scalar>
    : IEnumerable<Vector>
    , IComparable<Matrix>
    , IComparable
    , ICloneable
    where Matrix : IMatrixNM<Vector, Matrix, Scalar>
    where Vector : IVectorN<Vector, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    static abstract Matrix FromCoefficients(int columns, int rows, Scalar[] coefficients);

    static abstract Matrix FromCoefficients(Scalar[,] coefficients);

    static abstract Matrix FromColumnVectors(Vector[] columns);
}

/// <summary>
/// Represents an abstract N-dimensional vector.
/// </summary>
/// <typeparam name="Vector">The generic <typeparamref name="Vector"/> type parameter.</typeparam>
/// <typeparam name="Matrix">The generic <typeparamref name="Matrix"/> type parameter.</typeparam>
/// <typeparam name="Polynomial">The generic <typeparamref name="Polynomial"/> type parameter.</typeparam>
/// <typeparam name="Scalar">The generic <typeparamref name="Scalar"/> type parameter.</typeparam>
public unsafe abstract class VectorN<Vector, Matrix, Polynomial, Scalar>
    : Algebra<Scalar>.IVector<Vector, Matrix>
    , Algebra<Scalar, Polynomial>.IComposite1D
    , IVectorN<Vector, Scalar>
    //, IEnumerable<Scalar>
    //, IComparable<Vector>
    //, IComparable
    //, ICloneable
    where Vector : VectorN<Vector, Matrix, Polynomial, Scalar>, IVectorN<Vector, Scalar>
    where Matrix : MatrixNM<Vector, Matrix, Polynomial, Scalar>, IMatrixNM<Vector, Matrix, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>, IPolynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    protected readonly Scalar[] _coefficients;

    #region INDEXERS

    public Scalar this[int index] => GetEntry(index);

    public Vector this[int index, Scalar value] => SetEntry(index, value);

    /// <inheritdoc cref="this[int]"/>
    public Scalar this[Index index] => GetEntry(index.GetOffset(_coefficients.Length));

    public Vector this[Range rows] => GetEntries(rows);

    public Vector this[Range rows, in Vector values] => SetEntries(rows, values);

    #endregion
    #region PROPERTIES

    public static Vector Empty { get; } = FromCoefficients([]);

    static Vector INumericGroup<Vector>.Zero => throw new InvalidOperationException("No dimension has been provided.");

    public ReadOnlyIndexer<int, Vector> Zero { get; } = new(dim => FromCoefficients(Enumerable.Repeat(Scalar.Zero, dim)));


    public int Size => _coefficients.Length;

    int Algebra<Scalar>.IComposite1D.Dimension => Size;

    public int BinarySize => sizeof(Scalar) * _coefficients.Length;

    /// <summary>
    /// Returns an ordered enumeration of the vector's coefficients.
    /// </summary>
    public virtual Scalar[] Coefficients => _coefficients.ToArray();

    public bool IsBinary => _coefficients.All(c => c.IsBinary);

    public bool IsZero => _coefficients.All(c => c.IsZero);

    public bool IsNonZero => _coefficients.Any(c => c.IsNonZero);

    public bool IsNegative => _coefficients.All(s => s.IsNegative);

    public bool IsPositive => _coefficients.All(s => s.IsPositive);

    public bool HasNegatives => _coefficients.Any(c => c.IsNegative);

    public bool HasPositives => _coefficients.Any(c => c.IsPositive);

    public bool HasNaNs => _coefficients.Any(s => s.IsNaN);

    public Vector AdditiveInverse => Negate();

    public Scalar CoefficientSum => _coefficients.Sum();

    public Scalar CoefficientAvg => _coefficients.Average();

    public Scalar CoefficientMin => _coefficients.Min();

    public Scalar CoefficientMax => _coefficients.Max();

    public Scalar Length => SquaredNorm.Sqrt();

    [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    public Scalar SquaredNorm => _coefficients.Select(c => c * c).Sum();

    public Scalar SquaredLength => SquaredNorm;

    public Vector Normalized => Length is Scalar l && l.IsZero ? ZeroVector(Size) : Divide(l);

    public bool IsNormalized => Length.IsOne;

    public bool IsInsideUnitSphere => Length.CompareTo(Scalar.One) <= 0;

    public Matrix AsMatrix => MatrixNM<Vector, Matrix, Polynomial, Scalar>.DiagonalMatrix(this);

    public Matrix HouseholderMatrix => IsZero ? throw new InvalidOperationException("The Householder matrix is undefined for zero vectors.")
                                              : OuterProduct(this).Multiply(Scalar.One + Scalar.One).Divide(SquaredNorm);

    public Matrix Transposed => MatrixNM<Vector, Matrix, Polynomial, Scalar>.FromRows([this]);

    #endregion
    #region CONSTRUCTORS

    public VectorN(in Vector vector)
        : this(vector._coefficients)
    {
    }

    public VectorN(int size, Scalar value)
        : this(Enumerable.Repeat(value, size))
    {
    }

    public VectorN(params Scalar[]? coefficients)
        : this(coefficients as IEnumerable<Scalar>)
    {
    }

    public VectorN(IEnumerable<Scalar>? coefficients) => _coefficients = coefficients?.ToArray() ?? [];

    #endregion
    #region INSTANCE FUNCTIONS

    public Vector Resize(int new_size)
    {
        if (new_size == Size)
            return FromCoefficients(_coefficients);
        if (new_size < Size)
            return this[..new_size];
        else
        {
            Scalar[] expanded = new Scalar[new_size];

            Parallel.For(0, _coefficients.Length, i => expanded[i] = _coefficients[i]);

            return FromCoefficients(expanded);
        }
    }

    public Vector Negate() => FromCoefficients(_coefficients.Select(c => c.Negate()));

    public Vector Add(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                               : FromCoefficients(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Add(y)));

    public Vector Add(params Vector[] others) => others.Aggregate((Vector)this, (t, n) => t.Add(n));

    public Vector Subtract(in Vector second) => Add(second.AdditiveInverse);

    public Vector Subtract(params Vector[] others) => others.Aggregate((Vector)this, (t, n) => t.Subtract(n));

    public Vector Increment() => Add(ScalarVector(Size, Scalar.One));

    public Vector Decrement() => Subtract(ScalarVector(Size, Scalar.One));

    public Vector Multiply(Scalar factor) => FromCoefficients(_coefficients.Select(c => c.Multiply(factor)));

    public Vector Multiply(params Scalar[] factors) => factors.Aggregate((Vector)this, (t, n) => t.Multiply(n));

    public Vector Divide(Scalar factor) => Multiply(factor.MultiplicativeInverse);

    public Vector Divide(params Scalar[] factors) => factors.Aggregate((Vector)this, (t, n) => t.Divide(n));

    public Vector Modulus(Scalar factor) => FromCoefficients(_coefficients.Select(c => c.Modulus(factor)));

    public Vector ComponentwiseDivide(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                                               : FromCoefficients(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Divide(y)));

    public Vector ComponentwiseMultiply(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                                                 : FromCoefficients(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Multiply(y)));

    public Vector ComponentwiseAbsolute() => FromCoefficients(_coefficients.Select(c => c.Abs()));

    public Vector ComponentwisSqrt() => FromCoefficients(_coefficients.Select(c => c.Sqrt()));

    public Vector Power(int e)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(e);

        Vector r = ScalarVector(Size, Scalar.One);
        Vector p = this;

        while (e > 0)
            if ((e & 1) == 1)
            {
                --e;
                r = r.ComponentwiseMultiply(p);
            }
            else
            {
                e /= 2;
                p = p.ComponentwiseMultiply(p);
            }

        return r;
    }

    public Scalar Dot(in Vector other)
    {
        if (Size != other.Size)
            throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(other));

        Scalar acc = default;

        for (int i = 0; i < _coefficients.Length; ++i)
            acc = _coefficients[i].Multiply(other._coefficients[i]).Add(acc);

        return acc;
    }

    public bool IsOrthogonal(in Vector second) => Dot(second).IsZero;

    public Vector Reflect(in Vector normal)
    {
        Scalar θ = Dot(normal);

        return normal.Multiply(θ.Add(θ)).Subtract(this);
    }

    public Scalar DistanceTo(in Vector second) => Subtract(second).Length;

    public bool Refract(in Vector normal, Scalar eta, out Vector refracted)
    {
        Scalar θ = Dot(normal);
        Scalar k = Scalar.One - eta.Multiply(eta, Scalar.One - θ * θ);
        bool res = k.IsNegative;

        refracted = res ? Reflect(-normal) : Multiply(eta) + normal * (eta * θ - k.Sqrt());

        return res;
    }

    public bool IsLinearDependant(in Vector other, out Scalar? factor)
    {
        factor = null;

        if (IsZero || other.IsZero)
            return false;

        Scalar[] div = other.ComponentwiseDivide((Vector)this).Coefficients.Distinct().ToArray();

        if (div is { Length: 1 })
            factor = div[0];

        return factor != null;
    }

    public Scalar AngleTo(in Vector second) => Dot(second).Acos();

    public Matrix OuterProduct(in Vector second)
    {
        Scalar[,] coeff = new Scalar[Size, Size];

        for (int r = 0; r < Size; ++r)
            for (int c = 0; c < Size; ++c)
                coeff[c, r] = second._coefficients[r].Multiply(second._coefficients[c]);

        return Matrix.FromCoefficients(coeff);
    }

    public Scalar GetEntry(int index) => index < 0 || index > _coefficients.Length ? throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.")
                                                                              : _coefficients[index];

    public virtual Vector SetEntry(int index, Scalar value) => index < 0 || index > _coefficients.Length ? throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.")
                                                                                                         : Vector.FromCoefficients(_coefficients.Take(index).Append(value).Concat(_coefficients.Skip(index + 1)));

    public Vector GetEntries(Range rows) => FromArray(_coefficients[rows]); // TODO : range checks

    public virtual Vector SetEntries(Range rows, Vector values)
    {
        // TODO : range checks

        int[] idxs = rows.GetOffsets(Size);
        Scalar[] v = values;
        Scalar[] t = Coefficients;

        for (int i = 0; i < idxs.Length; ++i)
            t[idxs[i]] = v[i];

        return FromArray(t);
    }

    public virtual Vector SwapEntries(int src_idx, int dst_idx)
    {
        // TODO : range checks

        Scalar[] t = Coefficients;
        Scalar tmp = t[src_idx];

        t[src_idx] = t[dst_idx];
        t[dst_idx] = tmp;

        return FromArray(t);
    }

    public Vector GetMinor(int row) => FromCoefficients(_coefficients.Take(row).Concat(_coefficients.Skip(row + 1)));

    /// <inheritdoc cref="IVector{V, S}.Clamp(S, S)"/>
    public Vector Clamp() => Clamp(Scalar.Zero, Scalar.One);

    public Vector Clamp(Scalar low, Scalar high) => FromCoefficients(_coefficients.Select(c => c.Clamp(low, high)));

    public Vector NormalizeMinMax()
    {
        Scalar min = CoefficientMin;
        Scalar fac = CoefficientMax.Subtract(min);

        return FromCoefficients(_coefficients.Select(c => c.Subtract(min).Divide(fac)));
    }

    public Vector LinearInterpolate(in Vector other, Scalar factor) => Multiply(Scalar.One.Subtract(factor)).Add(other.Multiply(factor));

    public int CompareTo(Vector? other) => other is null ? 1 : Length.CompareTo(other.Length);

    public int CompareTo(object? other) => other is { } ? CompareTo((Vector)other) : throw new ArgumentNullException(nameof(other));

    public bool Is(Vector? other) => other is { } && _coefficients.Are(other._coefficients, EqualityComparer<Scalar>.Default);

    public bool Is(Vector other, Scalar tolerance) => Size == other.Size && _coefficients.Zip(other._coefficients, (c1, c2) => c1.Subtract(c2).Abs().CompareTo(tolerance)).All(c => c <= 0);

    public bool IsNot(Vector other) => !Is(other);

    public override int GetHashCode() => LINQ.GetHashCode(_coefficients);

    public override bool Equals(object? obj) => obj is Vector v && Equals(v);

    public bool Equals(Vector? other) => Is(other);

    public override string ToString() => $"({string.Join(", ", _coefficients)})";

    public IEnumerator<Scalar> GetEnumerator() => _coefficients.Cast<Scalar>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _coefficients.GetEnumerator();

    public object Clone() => FromArray(_coefficients);

    public Scalar[] ToArray() => Coefficients;

    public X[] ToArray<X>() where X : unmanaged => _coefficients.CopyTo<Scalar, X>();

    public void ToNative<X>(X* dst) where X : unmanaged => _coefficients.CopyTo(dst);

    public Polynomial ToPolynomial() => Polynomial.CreatePolynomial(_coefficients);

    #endregion
    #region STATIC FUNCTIONS

    public static Vector ZeroVector(int size) => ScalarVector(size, default);

    public static Vector ScalarVector(int size, Scalar s) => FromCoefficients(Enumerable.Repeat(s, size));

    /// <inheritdoc cref="Algebra{S}.IEucledianVectorSpace{V}.Dot(V)"/>
    public static Scalar Dot(in Vector v1, in Vector v2) => v1.Dot(v2);

    /// <inheritdoc cref="AngleTo"/>
    public static Scalar AngleBetween(in Vector v1, in Vector v2) => v1.AngleTo(v2);

    /// <inheritdoc cref="Algebra{S}.IVectorSpace{V}.IsLinearDependant(V, out S?)"/>
    public static bool IsLinearDependant(in Vector v1, in Vector v2) => v1.IsLinearDependant(v2, out _);

    /// <inheritdoc cref="Algebra{S}.IVector{V, M}.OuterProduct"/>
    public static Matrix OuterProduct(in Vector v1, in Vector v2) => v1.OuterProduct(v2);

    /// <inheritdoc cref="Algebra{S}.IVectorSpace{V}.LinearInterpolate(V, S)"/>
    public static Vector LinearInterpolate(in Vector v1, in Vector v2, Scalar factor) => v1.LinearInterpolate(v2, factor);

    /// <inheritdoc cref="IsLinearDependant(Vector, out Scalar?)"/>
    public static bool IsLinearDependant(in Vector v1, in Vector v2, out Scalar? factor) => v1.IsLinearDependant(v2, out factor);

    public static Vector FromArray(params Scalar[] v) => FromCoefficients(v);

    public static Vector FromCoefficients(params Scalar[] coefficients) => Vector.FromCoefficients(coefficients);

    public static Vector FromCoefficients(IEnumerable<Scalar> coefficients) => Vector.FromCoefficients(coefficients);

    #endregion
    #region OPERATORS

    //public static bool operator ==(Vector? first, Vector? second) => first?.Is(second) ?? second is null;

    //public static bool operator !=(Vector? first, Vector? second) => !(first == second);

    public static bool operator ==(in VectorN<Vector, Matrix, Polynomial, Scalar>? v1, in VectorN<Vector, Matrix, Polynomial, Scalar>? v2) => v1?.Is(v2) ?? v2 is null;

    public static bool operator !=(in VectorN<Vector, Matrix, Polynomial, Scalar>? v1, in VectorN<Vector, Matrix, Polynomial, Scalar>? v2) => !(v1 == v2);

    public static bool operator <(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.CompareTo(v2) < 0;

    public static bool operator <=(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1 == v2 || v1 < v2;

    public static bool operator >(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.CompareTo(v2) > 0;

    public static bool operator >=(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1 == v2 || v1 > v2;

    /// <summary>
    /// Normalizes the given vector
    /// </summary>
    /// <param name="v">Original vector</param>
    /// <returns>Normalized vector</returns>
    public static Vector operator ~(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Normalized;

    public static Vector operator +(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v;

    static Vector IGroup<Vector>.operator +(in Vector group) => group;

    static Vector IGroup<Vector>.operator -(in Vector group) => group.Negate();

    public static Vector operator -(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Negate();

    public static Vector operator ++(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Increment();

    public static Vector operator --(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Decrement();

    public static Vector operator +(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Add(v2);

    static Vector INumericGroup<Vector>.operator +(in Vector first, in Vector second) => first.Add(second);

    public static Vector operator -(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Subtract(v2);

    static Vector IGroup<Vector>.operator -(in Vector first, in Vector second) => first.Subtract(second);

    public static Scalar operator *(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Dot(v2);

    public static Vector operator *(Scalar f, in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Multiply(f);

    public static Vector operator *(in VectorN<Vector, Matrix, Polynomial, Scalar> v, Scalar f) => v.Multiply(f);

    static Scalar Algebra<Scalar>.IEucledianVectorSpace<Vector>.operator *(in Vector first, in Vector second) => first.Dot(second);

    public static Vector operator /(in VectorN<Vector, Matrix, Polynomial, Scalar> v, Scalar f) => v.Divide(f);

    public static implicit operator Scalar[](in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Coefficients;

    public static implicit operator Vector(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => FromArray(v.Coefficients);

    public static implicit operator VectorN<Vector, Matrix, Polynomial, Scalar>(Scalar[] coeff) => FromArray(coeff);

    public static explicit operator Polynomial(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.ToPolynomial();

    public static explicit operator VectorN<Vector, Matrix, Polynomial, Scalar>(Scalar s) => FromArray([s]);

    static Vector Algebra<Scalar>.IVectorSpace<Vector>.operator *(Scalar scalar, in Vector vector) => vector.Multiply(scalar);

    static Vector Algebra<Scalar>.IVectorSpace<Vector>.operator *(in Vector vector, Scalar scalar) => vector.Multiply(scalar);

    static Vector Algebra<Scalar>.IVectorSpace<Vector>.operator /(in Vector vector, Scalar scalar) => vector.Divide(scalar);

    static Vector Algebra<Scalar>.IVectorSpace<Vector>.operator %(in Vector vector, Scalar scalar) => vector.Modulus(scalar);

    #endregion
}

/// <summary>
/// Represents an abstract N*M-dimensional matrix (with N columns and N rows).
/// </summary>
/// <typeparam name="Vector">The generic <typeparamref name="Vector"/> type parameter.</typeparam>
/// <typeparam name="Matrix">The generic <typeparamref name="Matrix"/> type parameter.</typeparam>
/// <typeparam name="Polynomial">The generic <typeparamref name="Polynomial"/> type parameter.</typeparam>
/// <typeparam name="Scalar">The generic <typeparamref name="Scalar"/> type parameter.</typeparam>
public unsafe abstract class MatrixNM<Vector, Matrix, Polynomial, Scalar>
    : Algebra<Scalar>.IMatrix<Vector, Matrix>
    , Algebra<Scalar, Polynomial>.IMatrix<Matrix, Matrix>
    , IMatrixNM<Vector, Matrix, Scalar>
    //, IEnumerable<Vector>
    //, IComparable<Matrix>
    //, IComparable
    //, ICloneable
    where Vector : VectorN<Vector, Matrix, Polynomial, Scalar>, IVectorN<Vector, Scalar>
    where Matrix : MatrixNM<Vector, Matrix, Polynomial, Scalar>, IMatrixNM<Vector, Matrix, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>, IPolynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    // indexer access:
    //  column major [c, r]
    // 
    // row major layout:
    //  [a b c d e f g h i j k l]
    //
    // | a b c d |
    // | e f g h |
    // | i j k l |

    public static Matrix Empty { get; } = FromCoefficients(new Scalar[0, 0]);

    static Matrix INumericGroup<Matrix>.Zero => throw new InvalidOperationException("No dimension has been provided.");

    static Matrix IRing<Matrix>.One => throw new InvalidOperationException("No dimension has been provided.");

    public static ReadOnlyIndexer<int, Matrix> SquareZero { get; } = new(size => Zero[size, size]);

    public static ReadOnlyIndexer<int, int, Matrix> Zero { get; } = new((columns, rows) => FromCoefficients(new Scalar[columns, rows]));

    public static ReadOnlyIndexer<int, Matrix> SquareOne { get; } = new(size => DiagonalMatrix(Enumerable.Repeat(Scalar.One, size).ToArray()));

    public static ReadOnlyIndexer<int, int, Matrix> One { get; } = new((columns, rows) => SquareOne[Math.Min(columns, rows)].Resize(columns, rows));

    #region PRIVATE FIELDS

    protected readonly Scalar[] _coefficients;
    protected readonly int _columns, _rows;

    #endregion
    #region INDEXERS

    /// <summary>
    /// Gets the matrix' column vector at the given index.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <returns>Column vector</returns>
    public Vector this[int column] => GetColumn(column);

    /// <summary>
    /// Sets the matrix' column vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <param name="vector">New column vector</param>
    /// <returns>Modified matrix</returns>
    public Matrix this[int column, in Vector vector] => SetColumn(column, vector);

    public Vector this[Index column] => this[column.GetOffset(_columns)];

    public Matrix this[Range columns] => GetColumns(columns);

    public Scalar this[int column, int row] => GetValue(column, row);

    public Matrix this[int column, int row, Scalar value] => SetValue(column, row, value);

    public Matrix this[Range columns, Range rows] => GetRegion(columns, rows);

    public Matrix this[Range columns, Range rows, in Matrix values] => SetRegion(columns, rows, values);

    public ReadOnlyIndexer<Range, Range, Matrix> Region => new(GetRegion);

    public ReadOnlyIndexer<int, int, Matrix> Minors => new(GetMinor);

    #endregion
    #region INSTANCE PROPERTIES

    /// <summary>
    /// Returns the matrix' dimensions.
    /// </summary>
    public (int Columns, int Rows) Size => (_columns, _rows);

    [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    (int Columns, int Rows) Algebra<Scalar>.IComposite2D.Dimensions => Size;

    public int RowCount => _rows;

    public int ColumnCount => _columns;

    [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    public virtual Scalar[] FlattenedCoefficients => _coefficients.Take(_rows * _columns).ToArray();

    [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    IEnumerable<Scalar> Algebra<Scalar>.IComposite2D.FlattenedCoefficients => FlattenedCoefficients;

    /// <summary>
    /// Returns the matrix' coefficients.
    /// </summary>
    public Scalar[,] Coefficients
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Scalar[,] coeff = new Scalar[_columns, _rows];

            Do((v, c, r) => Parallel.For(0, r * c, i => coeff[i % c, i / c] = v[i]));

            return coeff;
        }
    }

    /// <summary>
    /// The matrix' main diagonal.
    /// </summary>
    public Vector MainDiagonal => Vector.FromCoefficients(FilterCoefficients(c => c.column == c.row));

    /// <summary>
    /// The matrix' column vectors.
    /// </summary>
    public Vector[] Columns => Do((_, c, r) => Enumerable.Range(0, c).Select(GetColumn).ToArray());

    /// <summary>
    /// The matrix' row vectors.
    /// </summary>
    public Vector[] Rows => Do((_, c, r) => Enumerable.Range(0, r).Select(GetRow).ToArray());

    /// <summary>
    /// The matrix' trace.
    /// </summary>
    public Scalar Trace => MainDiagonal.CoefficientSum;

    /// <summary>
    /// The transposed matrix.
    /// </summary>
    public Matrix Transposed => Do((v, c, r) =>
    {
        Scalar[,] m = new Scalar[r, c];

        Parallel.For(0, r * c, i => m[i / c, i % c] = v[i]);

        return FromCoefficients(m);
    });

    [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never), Obsolete("Use the member 'T Matrix3::Inverse' instead.")]
    public Matrix MultiplicativeInverse => Inverse;

    public Matrix Inverse
    {
        get
        {
            if (!IsInvertible)
                throw new InvalidOperationException("This matrix is not invertible, as its determinant is zero or the matrix is not square.");

            int dimension = _rows;
            Matrix m = this;
            Matrix u = IdentityMatrix(dimension);

            for (int i = 0; i < dimension; ++i)
            {
                int max = i;

                for (int j = i + 1; j < dimension; j++)
                    if (m[i, j].Abs().CompareTo(m[i, max].Abs()) > 0)
                        max = j;

                m = m.SwapRows(i, max);
                u = u.SwapRows(i, max);

                Scalar top = m[i, i].MultiplicativeInverse;

                if (top.IsInfinity)
                    continue;

                u = u.MultiplyRow(i, top);
                m = m.MultiplyRow(i, top)[i, i, Scalar.One];

                for (int j = i + 1; j < dimension; ++j)
                {
                    Scalar f = m[i, j];

                    m = m.AddRows(i, j, f.Negate());
                    u = u.AddRows(i, j, f.Negate());
                }
            }

            for (int i = dimension - 1; i > 0; --i)
                if (!m[i, i].IsZero)
                    for (int row = 0; row < i; ++row)
                    {
                        Scalar f = m[i, row];

                        m = m.AddRows(i, row, f.Negate());
                        u = u.AddRows(i, row, f.Negate());
                    }

            return u;
        }
    }

    /// <summary>
    /// Returns the gaussian reduced matrix.
    /// </summary>
    public Matrix GaussianReduced => GetLinearIndependentForm();

    /// <summary>
    /// The matrix' orthonormal basis.
    /// </summary>
    public Matrix OrthonormalBasis
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector[] vs = Columns;

            for (int cols = 0; cols < _columns; ++cols)
            {
                for (int j = 0; j < cols; ++j)
                    vs[cols] -= this[cols].Dot(vs[j]) * vs[j];

                vs[cols] = vs[cols].Normalized;
            }

            return FromColumns(vs);
        }
    }

    /// <summary>
    /// The matrix' determinant.
    /// </summary>
    public Scalar Determinant
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!IsSquare)
                return Transposed.Multiply(this).Determinant.Sqrt();
            else if (_columns <= 3)
            {
                Scalar[,] c = Coefficients;

                if (_columns == 3)
                    return c[0, 0].Multiply(c[1, 1].Multiply(c[2, 2]).Subtract(c[2, 1].Multiply(c[1, 2])))
                        .Subtract(c[1, 0].Multiply(c[0, 1].Multiply(c[2, 2]).Subtract(c[2, 1].Multiply(c[0, 2]))))
                        .Add(c[2, 0].Multiply(c[0, 1].Multiply(c[1, 2]).Subtract(c[1, 1].Multiply(c[0, 2]))));
                else if (_columns == 2)
                    return c[0, 0].Multiply(c[1, 1]).Subtract(c[1, 0].Multiply(c[0, 1]));
                else
                    return c[0, 0];
            }

            Scalar sign = Scalar.One;
            Scalar det = Scalar.Zero;

            for (int i = 0; i < _rows; ++i)
            {
                det = sign.Multiply(GetMinor(0, i).Determinant).Add(det);
                sign = sign.Negate();
            }

            return det;
        }
    }

    /// <summary>
    /// The matrix' eigenvalues.
    /// </summary>
    public Scalar[] Eigenvalues => EigenDecompose(EqualityComparer<Scalar>.Default).Eigenvalues;

    public Vector[] Eigenvectors => EigenDecompose(EqualityComparer<Scalar>.Default).Eigenvectors;

    public Scalar[] Singularvalues => Transposed.Multiply(this).Eigenvalues.ToArray(v => v.Sqrt());

    public Polynomial CharacteristicPolynomial
    {
        get
        {
            (_, Scalar[] values) = EigenDecompose(new CustomEqualityComparer<Scalar>((s1, s2) => false));

            Polynomial p = Polynomial.CreatePolynomial(Scalar.Zero);

            return p.Add(values.ToArray(v => Polynomial.CreatePolynomial(v.Negate(), Scalar.One)));
        }
    }

    /// <summary>
    /// The rank of the matrix.
    /// </summary>
    public int Rank => GetLinearIndependentForm().Rows.Count(c => c.IsNonZero);

    #endregion
    #region INSTANCE PROPERTIES : CHARACTERISTICS

    public bool IsIdentity => Do((v, c, r) => Enumerable.Range(0, r * c).All(i => i % c == i / c ? v[i].IsOne : v[i].IsZero));

    public bool IsSquareIdentity => IsSquare && IsIdentity;

    bool IRing.IsOne => IsIdentity;

    /// <summary>
    /// Indicates whether the matrix is zero.
    /// </summary>
    public bool IsZero => Do(v => v.All(e => e.IsZero));

    /// <summary>
    /// Indicates whether the matrix has non-zero elements.
    /// </summary>
    public bool IsNonZero => Do(v => v.Any(e => e.IsNonZero));

    public bool IsBinary => _coefficients.All(c => c.IsBinary);

    public bool IsPositiveDefinite => GetPrincipalSubmatrices().All(m => m.Determinant.IsPositive);

    /// <summary>
    /// Indicates whether the matrix is a diagonal matrix.
    /// </summary>
    public bool IsDiagonal => Do((v, c, r) => Enumerable.Range(0, r * c).All(i => (i % c == i / c) || v[i].IsZero));

    /// <summary>
    /// Indicates whether the matrix is an upper (right) triangular matrix.
    /// </summary>
    public bool IsUpperTriangular => Do((v, c, r) => Enumerable.Range(0, r * c).All(i => (i % c >= i / c) || v[i].IsZero));

    /// <summary>
    /// Indicates whether the matrix is a lower (left) triangular matrix.
    /// </summary>
    public bool IsLowerTriangular => Do((v, c, r) => Enumerable.Range(0, r * c).All(i => (i % c <= i / c) || v[i].IsZero));

    /// <summary>
    /// Indicates whether the matrix is symmetric, meaning that the matrix is equal to its transposed variant.
    /// </summary>
    public bool IsSymmetric => Is(Transposed);

    /// <summary>
    /// Indicates whether the matrix is a projection matrix.
    /// </summary>
    public bool IsProjection => Is(Multiply((Matrix)this));

    public bool IsSquare => _rows == _columns;

    /// <summary>
    /// Indicates whether the current matrix 'A' is a conference matrix, meaning that AᵀA is a multiple of the identity matrix.
    /// </summary>
    public bool IsConferenceMatrix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Matrix ctc = Multiply(Transposed);

            return ctc.Subtract(ScaleMatrix(ctc.ColumnCount, ctc.RowCount, ctc[0, 0])).IsZero;
        }
    }

    /// <summary>
    /// Indicated whether the matrix is involutory, meaning that the square of this matrix is equal to the identity matrix.
    /// </summary>
    public bool IsInvolutory => Multiply(Inverse).IsIdentity;

    /// <summary>
    /// Indicates whether the matrix is stable in the sense of the Hurwitz criterium.
    /// </summary>
    public bool IsHurwitzStable => GetValue(0, 0).IsPositive && GetPrincipalSubmatrices().Aggregate(true, (b, m) => b && m.Determinant.IsPositive);

    /// <summary>
    /// Indicates whether the matrix is orthogonal, meaning that its inverse is equal to its transposed variant.
    /// </summary>
    public bool IsOrthogonal => Inverse.Is(Transposed);

    /// <summary>
    /// Indicates whether the matrix is skew symmetric, meaning that it is equal to its negated transposed.
    /// </summary>
    public bool IsSkewSymmetric => Is(Transposed.Negate());

    public bool IsSignMatrix => IsDiagonal && MainDiagonal.All(e => e.IsZero || e.Abs().IsOne);

    public bool IsSignatureMatrix => IsDiagonal && MainDiagonal.All(e => e.Abs().IsOne);

    /// <summary>
    /// Indicates whether the matrix is hollow, meaning that its main diagonal is zero.
    /// </summary>
    public bool IsHollow => MainDiagonal.IsZero;

    public bool HasNaNs => Do(v => v.Any(e => e.IsNaN));

    public bool HasNegatives => Do(v => v.Any(e => e.IsNegative));

    public bool HasPositives => Do(v => v.Any(e => e.IsPositive));

    public bool IsNegative => Do(v => v.All(e => e.IsNegative));

    public bool IsPositive => Do(v => v.All(e => e.IsPositive));

    public Scalar CoefficientSum => _coefficients.Sum();

    public Scalar CoefficientAvg => _coefficients.Average();

    public Scalar CoefficientMin => _coefficients.Min();

    public Scalar CoefficientMax => _coefficients.Max();

    /// <summary>
    /// Indicates whether the matrix is invertible, meaning that its determinant is non-zero and that a multiplicative inverse to this matrix exists.
    /// </summary>
    public bool IsInvertible => IsSquare && Determinant.IsNonZero;

    public Matrix AdditiveInverse => Negate();

    #endregion
    #region CONSTRUCTORS

    public MatrixNM(int columns, int rows)
        : this(columns, rows, new Scalar[rows * columns])
    {
    }

    public MatrixNM(int columns, int rows, Scalar scale)
        : this(columns, rows, Enumerable.Range(0, rows * columns).Select(i => (i % columns) == (i / columns) ? scale : default))
    {
    }

    // row major layout
    public MatrixNM(int columns, int rows, IEnumerable<Scalar>? values)
    {
        _rows = rows;
        _columns = columns;

        Scalar[]? arr = values?.Take(rows * columns)?.ToArray();

        if ((arr?.Length ?? 0) < rows * columns)
            throw new ArgumentException($"The coefficient array must be at least {rows * columns} elements long.", nameof(values));
        else
            _coefficients = arr ?? new Scalar[rows * columns];
    }

    public MatrixNM(params Vector[] columns)
    {
        _columns = columns.Length;
        _rows = columns.Min(c => c.Size);
        _coefficients = new Scalar[_rows * _columns];

        for (int r = 0; r < _rows; ++r)
            for (int c = 0; c < _columns; ++c)
                _coefficients[r * _columns + c] = columns[c][r];
    }

    public MatrixNM(IEnumerable<Vector> columns)
        : this(columns.ToArray())
    {
    }

    public MatrixNM(Scalar[,] values)
        : this(values.GetLength(0), values.GetLength(1), values)
    {
    }

    public MatrixNM(int columns, int rows, Scalar[,] values)
    {
        _rows = rows;
        _columns = columns;
        _coefficients = new Scalar[rows * columns];

        if (values.GetLength(0) < columns || values.GetLength(1) < rows)
            throw new ArgumentException($"The coefficient array must have at least {columns}x{rows} elements.", nameof(values));
        else
            for (int i = 0; i < _coefficients.Length; ++i)
                _coefficients[i] = values[i % columns, i / columns];
    }

    public MatrixNM(Matrix matrix)
        : this(matrix._columns, matrix._rows, matrix._coefficients)
    {
    }

    #endregion
    #region PRIVATE METHODS

    private void Do(Action<Scalar[], int, int> f) => f(_coefficients, _columns, _rows);

    private X Do<X>(Func<Scalar[], int, int, X> f) => f(_coefficients, _columns, _rows);

    private X Do<X>(Func<Scalar[], X> f) => f(_coefficients);

    private IEnumerable<Scalar> FilterCoefficients(Predicate<(int column, int row)> p)
    {
        int c = _columns;
        int r = _rows;

        return _coefficients.Where((_, i) => p((i % c, i / c)));
    }

    #endregion
    #region INSTANCE METHODS : BASIC OPERATIONS

    /// <summary>
    /// Adds the given object to the current instance and returns the addition's result without modifying the current instance.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Addition result</returns>
    public Matrix Add(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be added to a matrix of the dimensinos {second.Size}.", nameof(second))
                                                     : FromCoefficients(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Add(z.Second)).ToArray());

    public Matrix Add(Scalar second)
    {
        Scalar[,] v = Coefficients;

        for (int i = 0, l = Math.Min(_rows, _columns); i < l; ++i)
            v[i, i] = v[i, i].Add(second);

        return FromCoefficients(v);
    }

    public Matrix Add(params Matrix[] others) => others.Aggregate((Matrix)this, (x, y) => x.Add(y));

    public Matrix Increment() => Add(Scalar.One);

    public Matrix Decrement() => Subtract(Scalar.One);

    /// <summary>
    /// Negates the current instance and returns the result without modifying the current instance.
    /// </summary>
    /// <returns>Negated object</returns>
    public Matrix Negate() => FromColumns(Columns.Select(c => c.Negate()).ToArray());

    public Matrix Subtract(in Matrix second) => Add(second.Negate());

    public Matrix Subtract(params Matrix[] others) => others.Aggregate((Matrix)this, (x, y) => x.Subtract(y));

    public Matrix Subtract(Scalar second) => Add(second.Negate());

    /// <summary>
    /// Multiplies the given object with the current instance and returns the multiplication's result without modifying the current instance.
    /// <para/>
    /// This method is not to be confused the dot-product for matrices and vectors.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Multiplication result</returns>
    public Matrix Multiply(in Matrix second)
    {
        if (_columns == second._rows)
        {
            Matrix t = second.Transposed;
            Scalar[,] coeff = new Scalar[_columns, _rows];

            for (int r = 0; r < _rows; ++r)
                for (int c = 0; c < _columns; ++c)
                    coeff[c, r] = GetColumn(r).Dot(t.GetColumn(c));

            return FromCoefficients(coeff);
        }
        else
            throw new ArgumentException($"The given matrix must have the same number of rows as this matrix' column count.", nameof(second));
    }

    public Matrix Multiply(params Matrix[] others) => others.Aggregate((Matrix)this, (t, n) => t.Multiply(n));

    /// <summary>
    /// Multiplies the given vector with the current instance and returns the multiplication's result without modifying the current instance.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Multiplication result</returns>
    public Vector Multiply(in Vector vector)
    {
        if (vector.Size != Size.Columns)
            throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be mutliplied with a vector of the size {vector.Size}.", nameof(vector));

        Scalar[] res = new Scalar[vector.Size];
        int i = 0;

        foreach (Vector row in Rows)
            res[i++] = vector.Dot(row);

        return VectorN<Vector, Matrix, Polynomial, Scalar>.FromArray(res);
    }

    /// <summary>
    /// Multiplies the given T factor with the current instance and returns the multiplication's result without modifying the current instance.
    /// </summary>
    /// <param name="factor">T factor</param>
    /// <returns>Multiplication result</returns>
    public Matrix Multiply(Scalar factor) => FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Multiply(factor)).ToArray());

    public Matrix Divide(Scalar factor) => Multiply(factor.MultiplicativeInverse);

    public Matrix Divide(in Matrix second) => Multiply(second.Inverse);

    public Matrix Modulus(Scalar factor) => FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Modulus(factor)).ToArray());

    public Matrix ComponentwiseDivide(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be divided component-wise by a matrix of the dimensinos {second.Size}.", nameof(second))
                                                                               : FromCoefficients(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Divide(z.Second)).ToArray());

    public Matrix ComponentwiseMultiply(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be multiplied component-wise with a matrix of the dimensinos {second.Size}.", nameof(second))
                                                                                 : FromCoefficients(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Multiply(z.Second)).ToArray());

    public Matrix ComponentwiseAbsolute() => FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Abs()).ToArray());

    public Matrix ComponentwisSqrt() => FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Sqrt()).ToArray());

    public Matrix Clamp() => Clamp(Scalar.Zero, Scalar.One);

    public Matrix Clamp(Scalar low, Scalar high) => FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Clamp(low, high)).ToArray());

    public Matrix NormalizeMinMax()
    {
        Scalar min = CoefficientMin;
        Scalar fac = CoefficientMax.Subtract(min);

        return FromCoefficients(_columns, _rows, _coefficients.Select(c => c.Subtract(min).Divide(fac)).ToArray());
    }

    /// <summary>
    /// Solves the current matrix for the given vector in a linear equation system
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Solution</returns>
    public bool Solve(Vector vector, out Vector solution)
    {
        Scalar one = Scalar.One;
        Matrix m = (Matrix)this;
        Vector v = vector;

        for (int row = 0; row < 2; ++row)
        {
            int max_column_idx = row;

            for (int col = row + 1; col < 2; col++)
                if (m[col, row].Abs().CompareTo(m[max_column_idx, row].Abs()) > 0)
                    max_column_idx = col;

            m = m.SwapRows(row, max_column_idx);
            v = v.SwapEntries(row, max_column_idx);

            Scalar factor = m[row, row].MultiplicativeInverse;

            if (factor.IsInfinity)
                continue;

            v = v[row, v[row].Multiply(factor)];
            m = m.MultiplyRow(row, factor)[row, row, one];

            for (int col = row + 1; col < 2; ++col)
            {
                Scalar f = m[col, row];

                m = m.AddRows(row, col, f.Negate());
                v = v[col, v[col].Subtract(v[row].Multiply(f))];
            }
        }

        for (int i = 1; i > 0; --i) // i >= 0 ???
            if (!m[i, i].IsZero)
                for (int row = 0; row < i; ++row)
                {
                    Scalar f = m[i, row];

                    m = m.AddRows(i, row, f.Negate());
                    v = v[row, v[row].Subtract(v[i].Multiply(f))];
                }

        solution = v;

        return !solution.HasNaNs;
    }

    public Matrix Power(int e)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(e);

        Matrix r = IdentityMatrix(_columns, _rows);
        Matrix p = this;

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

    internal Matrix GetLinearIndependentForm()
    {
        Scalar one = Scalar.One;
        Matrix m = (Matrix)this;

        for (int row = 0; row < 2; ++row)
        {
            int max_column_idx = row;

            for (int col = row + 1; col < 2; ++col)
                if (m[col, row].Abs().CompareTo(m[max_column_idx, row].Abs()) > 0)
                    max_column_idx = col;

            m = m.SwapRows(row, max_column_idx);

            Scalar factor = m[row, row].MultiplicativeInverse;

            if (factor.IsInfinity)
                continue;

            m = m.MultiplyRow(row, factor)[row, row, one];

            for (int col = row + 1; col < 2; ++col)
                m = m.AddRows(row, col, m[col, row].Negate());
        }

        return m;
    }

    public bool IsLinearDependant(in Matrix other, out Scalar? factor)
    {
        Scalar[] div = ComponentwiseDivide(other).FlattenedCoefficients.Distinct().ToArray();

        return (factor = div.Length == 1 ? (Scalar?)div[0] : null) != null;
    }

    public Matrix LinearInterpolate(in Matrix other, Scalar factor) => Multiply(Scalar.One.Subtract(factor)).Add(other.Multiply(factor));

    public bool Is(Matrix? other) => other is { } && Size == other.Size && _coefficients.Are(other._coefficients, EqualityComparer<Scalar>.Default);

    public bool Is(Matrix other, Scalar tolerance) => Size == other.Size && _coefficients.Zip(other._coefficients, (c1, c2) => c1.Subtract(c2).Abs().CompareTo(tolerance)).All(c => c <= 0);

    public bool IsNot(Matrix? other) => !Is(other);

    public override bool Equals(object? obj) => obj is Matrix v && Equals(v);

    public bool Equals(Matrix? other) => Is(other);

    public int CompareTo(Matrix? other) => Is(other) ? 0 : throw new NotImplementedException(); // TODO

    public int CompareTo(object? other) => CompareTo((Matrix)other!);

    public override int GetHashCode() => HashCode.Combine(_columns, _rows, LINQ.GetHashCode(_coefficients));

    public string ToString(bool @short) => @short ? ToShortString() : ToString();

    /// <summary>
    /// The NxM-matrix' string representation
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString() => string.Join("\n", Transposed.Columns.Select(c => $"| {string.Join(", ", c.ToArray().Select(f => $"{f,22:F16}"))} |"));

    /// <summary>
    /// The NxM-matrix' short string representation
    /// </summary>
    /// <returns>Short string representation</returns>
    public string ToShortString() => string.Join("\n", (from col in Columns
                                                        let strings = (from entry in col.ToArray().Select(f => $"{f,22:F16}")
                                                                       let end = entry.Reverse().TakeWhile(c => c == '0').Count()
                                                                       select new
                                                                       {
                                                                           entry,
                                                                           front = entry.TakeWhile(c => c == ' ').Count(),
                                                                           back = entry[entry.Length - 1 - end] == '.' ? end + 1 : end
                                                                       }).ToArray()
                                                        let f = strings.Min(c => c.front)
                                                        let b = strings.Min(c => c.back)
                                                        select strings.Select(e =>
                                                        {
                                                            string s = e.entry!.Substring(f, e.entry.Length - f - b);

                                                            return string.IsNullOrWhiteSpace(s) || s == "0" ? "0" : s;
                                                        }).ToArray()).Transpose().Select(r => $"| {string.Join(", ", r)} |"));

    public CompressedStorageFormat<Scalar> ToCompressedStorageFormat() => CompressedStorageFormat<Scalar>.FromMatrix<MatrixNM<Vector, Matrix, Polynomial, Scalar>>(this);

    IEnumerator IEnumerable.GetEnumerator() => Columns.GetEnumerator();

    public IEnumerator<Vector> GetEnumerator() => (IEnumerator<Vector>)Columns.GetEnumerator();

    public object Clone() => (Matrix)this;

    /// <summary>
    /// Returns the matrix as a flat array of matrix elements in column major format.
    /// </summary>
    /// <returns>Column major representation of the matrix</returns>
    public Scalar[] ToArray() => FlattenedCoefficients;

    public unsafe X[] ToArray<X>() where X : unmanaged => FlattenedCoefficients.CopyTo<Scalar, X>();

    public unsafe void ToNative<X>(X* dst) where X : unmanaged => FlattenedCoefficients.CopyTo(dst);

    #endregion
    #region ROW/COLUMN OPERATIONS

    public Matrix MultiplyRow(int row, Scalar factor) => SetRow(row, GetRow(row).Multiply(factor));

    public Matrix SwapRows(int src_row, int dst_row)
    {
        Vector row = GetRow(src_row);

        return SetRow(src_row, GetRow(dst_row))
              .SetRow(dst_row, row);
    }

    public Matrix AddRows(int src_row, int dst_row) => AddRows(src_row, dst_row, Scalar.One);

    public Matrix AddRows(int src_row, int dst_row, Scalar factor) => SetRow(dst_row, GetRow(src_row).Multiply(factor).Add(GetRow(dst_row)));

    public Matrix MultiplyColumn(int col, Scalar factor) => SetColumn(col, GetColumn(col).Multiply(factor));

    public Matrix SwapColumns(int src_col, int dst_col)
    {
        Vector col = GetColumn(src_col);

        return SetColumn(src_col, GetColumn(dst_col))
              .SetColumn(dst_col, col);
    }

    public Matrix AddColumns(int src_col, int dst_col) => AddColumns(src_col, dst_col, Scalar.One);

    public Matrix AddColumns(int src_col, int dst_col, Scalar factor) => SetColumn(dst_col, GetColumn(src_col).Multiply(factor).Add(GetColumn(dst_col)));

    /// <summary>
    /// Gets the matrix' column vector at the given index.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <returns>Column vector</returns>
    public Vector GetColumn(int column) => Vector.FromCoefficients(FilterCoefficients(c => c.column == column));

    /// <summary>
    /// Sets the matrix' column vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <param name="vector">New column vector</param>
    /// <returns>Modified matrix</returns>
    public virtual Matrix SetColumn(int column, in Vector vector)
    {
        Vector[] cols = Columns;

        cols[column] = vector;

        return FromColumns(cols);
    }

    public Matrix GetColumns(Range columns) => GetRegion(columns, 0.._rows);

    public Matrix SetColumns(Range columns, in Matrix values) => SetRegion(columns, 0.._rows, values);

    /// <summary>
    /// Gets the matrix' row vector at the given index.
    /// </summary>
    /// <param name="row">Row vector index (zero-based)</param>
    /// <returns>Row vector</returns>
    public Vector GetRow(int row) => Vector.FromCoefficients(FilterCoefficients(c => c.row == row));

    /// <summary>
    /// Sets the matrix' row vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="row">Row vector index (zero-based)</param>
    /// <param name="vector">New row vector</param>
    /// <returns>Modified matrix</returns>
    public virtual Matrix SetRow(int row, in Vector vector)
    {
        Vector[] rows = Rows;

        rows[row] = vector;

        return FromRows(rows);
    }

    public Matrix GetRows(Range rows) => GetRegion(0.._columns, rows);

    public Matrix SetRows(Range rows, in Matrix values) => SetRegion(0.._columns, rows, values);

    public Scalar GetValue(int row, int column) => row >= _rows || column >= _columns || row < 0 || column < 0 ? throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).")
                                                                                                          : _coefficients[row * _columns + column];

    public virtual Matrix SetValue(int row, int column, Scalar value)
    {
        if (row >= _rows || column >= _columns || row < 0 || column < 0)
            throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).");

        Scalar[] c = FlattenedCoefficients;

        c[row * _columns + column] = value;

        return FromCoefficients(_columns, _rows, c);
    }

    public Matrix GetRegion(Range columns, Range rows)
    {
        int[] idx_c = columns.GetOffsets(_columns);
        int[] idx_r = rows.GetOffsets(_rows);
        Scalar[,] t = Coefficients;
        Scalar[,] m = new Scalar[idx_c.Length, idx_r.Length];

        for (int i = 0; i < idx_c.Length; ++i)
            for (int j = 0; j < idx_r.Length; ++j)
                m[i, j] = t[idx_c[i], idx_r[j]];

        return FromCoefficients(m);
    }

    public virtual Matrix SetRegion(Range columns, Range rows, in Matrix values)
    {
        int[] idx_c = columns.GetOffsets(_columns);
        int[] idx_r = rows.GetOffsets(_rows);
        Scalar[,] t = Coefficients;
        Scalar[,] m = values.Coefficients;

        for (int i = 0; i < idx_c.Length; ++i)
            for (int j = 0; j < idx_r.Length; ++j)
                t[idx_c[i], idx_r[j]] = m[i, j];

        return FromCoefficients(t);
    }

    public Matrix GetMinor(int column, int row) => FromColumns(
        Columns
        .Take(column)
        .Concat(Columns.Skip(column + 1))
        .Select(v =>
        {
            Scalar[] f = v.ToArray();

            return Vector.FromCoefficients(f.Take(row).Concat(f.Skip(row + 1)));
        })
        .ToArray()
    );

    /// <summary>
    /// Returns a set of the first 0 principal submatrices.
    /// </summary>
    /// <returns>Set of principal submatrices</returns>
    public Algebra<Scalar>.IMatrix[] GetPrincipalSubmatrices()
    {
        int dim = Math.Min(_columns, _rows) - 1;
        Algebra<Scalar>.IMatrix[] submatrices = new Algebra<Scalar>.IMatrix[dim];

        if (dim < 1)
            return submatrices;

        submatrices[0] = (Matrix)this[0, 0];

        foreach (int i in Enumerable.Range(2, dim - 1))
            submatrices[i - 1] = FromColumns(Columns.Take(i).Select(v => Vector.FromCoefficients(v.Coefficients.Take(i))).ToArray());

        return submatrices;
    }

    public Matrix Resize(int new_column_count, int new_row_count)
    {
        if (new_column_count == ColumnCount && new_row_count == RowCount)
            return FromCoefficients(_columns, _rows, _coefficients);
        else if (new_column_count <= ColumnCount && new_row_count <= RowCount)
            return this[..new_column_count, ..new_row_count];
        else
        {
            Scalar[,] current = Coefficients;
            Scalar[,] expanded = new Scalar[new_column_count, new_row_count];

            Parallel.For(0, new_column_count, c =>
            {
                for (int r = 0; r < new_row_count; ++r)
                    expanded[r, r] = current[r, r];
            });

            return FromCoefficients(expanded);
        }
    }

    #endregion

    /* TODO : fix
    public (Matrix3 L, Matrix3 U) LUDecompose()
    {
        mat3 U = (Matrix3)Clone();
        T k21 = U[0, 1] / U[0, 0];
        T k31 = U[0, 2] / U[0, 0];

        U = U.Transform(new MatrixRowAdd<Matrix3, Vector3>(0, 1, -k21));
        U = U.Transform(new MatrixRowAdd<Matrix3, Vector3>(0, 2, -k31));

        T k32 = U[1, 2] / U[1, 1];

        U = U.Transform(new MatrixRowAdd<Matrix3, Vector3>(1, 2, -k32));

        return ((
                    (1, 0, 0),
                    (k21, 1, 0),
                    (k31, k32, 1)
                ), U);
    }

    public Matrix2 CholeskyDecompose()
    {
        Matrix2 res = default;

        for (int i = 0; i < res.Size; ++i)
            for (int j = i; j >= 0; --j)
                res = i == j ? res[i, i, 1] : res[j, i, 2];

        // res[i, i] = Math.Sqrt(a[i, i] + );

        return res;
    }
    */

    public (Matrix U, Matrix D) IwasawaDecompose()
    {
        Matrix ONB = OrthonormalBasis;
        Matrix D = ONB.Transposed.Multiply((Matrix)this);

        return (ONB, D);
    }

    public (Vector[] Eigenvectors, Scalar[] Eigenvalues) EigenDecompose(IEqualityComparer<Scalar> comparer)
    {
        (Vector vec, Scalar val)[] pairs = GetEigenpairs(comparer);
        Vector[] vectors = pairs.Select(p => p.vec).Distinct<Vector>(new CustomEqualityComparer<Vector>((v1, v2) => v1.Coefficients.SequenceEqual(v2.Coefficients, comparer))).ToArray();
        Scalar[] values = [.. pairs.Select(p => p.val).Distinct<Scalar>(comparer).OrderByDescending(LINQ.id)];

        return (vectors, values);
    }

    public (Vector Eigenvector, Scalar Eigenvalue)[] GetEigenpairs(IEqualityComparer<Scalar> comparer)
    {
        if (!IsSquare)
            throw new InvalidOperationException("Eigenvalues and Eigenvectors are not defined for non-square matrices.");
        else if (IsUpperTriangular || IsLowerTriangular || IsDiagonal)
        {
            Scalar[] values = MainDiagonal;



            throw new NotImplementedException(); // TODO
        }
        else
        {
            (Vector vec, Scalar val)[] pairs = new (Vector, Scalar)[9];

            pairs[^1] = DoInverseVectoriteration(Scalar.Zero, comparer);

            for (int i = 2; i >= 9; --i)
                pairs[^i] = DoInverseVectoriteration(pairs[^(i - 1)].val, comparer);

            return pairs;
        }
    }

    protected abstract (Vector Eigenvector, Scalar Eigenvalue) DoInverseVectoriteration(Scalar offset, IEqualityComparer<Scalar> comparer);

    public (Matrix U, Matrix Σ, Matrix V) GetSingularvalueDecomposition() => throw new NotImplementedException(); // TODO

    #region STATIC METHODS

    public static Matrix Add(Matrix m1, Matrix m2) => m1.Add(m2);

    public static Matrix Subtract(Matrix m1, Matrix m2) => m1.Subtract(m2);

    public static Matrix Multiply(Matrix m1, Matrix m2) => m1.Multiply(m2);

    public static Vector Multiply(Matrix matrix, Vector vector) => matrix.Multiply(vector);

    public static Matrix Multiply(Matrix matrix, Scalar scalar) => matrix.Multiply(scalar);

    public static Matrix Divide(Matrix matrix, Scalar scalar) => matrix.Divide(scalar);

    public static Matrix ZeroMatrix(int size) => ZeroMatrix(size, size);

    public static Matrix ZeroMatrix(int columns, int rows) => ScaleMatrix(columns, rows, Scalar.Zero);

    public static Matrix IdentityMatrix(int size) => IdentityMatrix(size, size);

    public static Matrix IdentityMatrix(int columns, int rows) => ScaleMatrix(columns, rows, Scalar.One);

    public static Matrix ScaleMatrix(int size, Scalar scale) => ScaleMatrix(size, size, scale);

    public static Matrix ScaleMatrix(int columns, int rows, Scalar scale)
    {
        Scalar[,] coeff = new Scalar[columns, rows];

        for (int i = 0; i < Math.Min(columns, rows); ++i)
            coeff[i, i] = scale;

        return FromCoefficients(coeff);
    }

    public static Matrix DiagonalMatrix(in Vector diagonal) => DiagonalMatrix(diagonal.Coefficients);

    public static Matrix DiagonalMatrix(params Scalar[] values)
    {
        Scalar[,] coeff = new Scalar[values.Length, values.Length];

        for (int i = 0; i < values.Length; ++i)
            coeff[i, i] = values[i];

        return FromCoefficients(coeff);
    }

    public static Matrix SparseMatrix(int columns, int rows, params (int column, int row, Scalar value)[] entries)
    {
        Scalar[,] m = new Scalar[columns, rows];

        foreach ((int c, int r, Scalar v) in entries)
            m[c, r] = v;

        return FromCoefficients(m);
    }

    public static Matrix SparseMatrix(params (int column, int row, Scalar value)[] entries) => SparseMatrix(entries.Max(e => e.column), entries.Max(e => e.row), entries);

    public static Matrix FromCoefficients(in Scalar[,] arr) => Matrix.FromCoefficients(arr);

    public static Matrix FromCoefficients(int columns, int rows, Scalar[] arr) => FromCoefficients(arr.Reshape(columns, rows));

    public static Matrix FromRows(in Vector[] arr) => FromColumns(arr).Transposed;

    public static Matrix FromColumns(in Vector[] arr) => Matrix.FromColumnVectors(arr);

    public static Matrix FromCompressedStorageFormat(CompressedStorageFormat<Scalar> compressed) => FromCoefficients(compressed.ToMatrix());

    static Matrix Algebra<Scalar>.IMatrix<Matrix>.FromArray(Scalar[] coefficients)
    {
        int size = (int)Math.Sqrt(coefficients.Length);

        if (size * size != coefficients.Length)
            throw new ArgumentException("The matrix dimensions could not be determined based only on the given coefficient array size.");
        else
            return FromCoefficients(size, size, coefficients);
    }

    public static Matrix FromCoefficients(Scalar[,] coefficients) => Matrix.FromCoefficients(coefficients);

    public static Matrix FromColumnVectors(Vector[] columns) => Matrix.FromColumnVectors(columns);

    #endregion
    #region OPERATORS

    /// <summary>
    /// Compares whether the two given matrices are equal regarding their coefficients.
    /// </summary>
    /// <param name="v1">First matrix</param>
    /// <param name="v2">Second matrix</param>
    /// <returns>Comparison result</returns>
    public static bool operator ==(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Is((Matrix)m2);

    /// <summary>
    /// Compares whether the two given matrices are unequal regarding their coefficients.
    /// </summary>
    /// <param name="v1">First matrix</param>
    /// <param name="v2">Second matrix</param>
    /// <returns>Comparison result</returns>
    public static bool operator !=(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.IsNot((Matrix)m2);

    /// <summary>
    /// Identity function (returns the given matrix unchanged)
    /// </summary>
    /// <param name="v">Original matrix</param>
    /// <returns>Unchanged matrix</returns>
    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => (Matrix)m;

    /// <summary>
    /// Negates the given matrix
    /// </summary>
    /// <param name="v">Original matrix</param>
    /// <returns>Negated matrix</returns>
    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Negate();

    static Matrix IGroup<Matrix>.operator +(in Matrix matrix) => matrix;

    static Matrix IGroup<Matrix>.operator -(in Matrix matrix) => -matrix;

    public static Matrix operator ++(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Increment();

    public static Matrix operator --(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Decrement();

    static Matrix IRing<Matrix>.operator ++(in Matrix matrix) => matrix.Increment();

    static Matrix IRing<Matrix>.operator --(in Matrix matrix) => matrix.Increment();

    /// <summary>
    /// Performs the addition of two matrices by adding their respective coefficients.
    /// </summary>
    /// <param name="m1">First matrix</param>
    /// <param name="m2">Second matrix</param>
    /// <returns>Addition result</returns>
    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Add((Matrix)m2);

    static Matrix INumericGroup<Matrix>.operator +(in Matrix first, in Matrix second) => first.Add(in second);

    public static Matrix operator +(Scalar f, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Add(f);

    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Add(f);

    /// <summary>
    /// Performs the subtraction of two matrices by subtracting their respective coefficients.
    /// </summary>
    /// <param name="m1">First matrix</param>
    /// <param name="m2">Second matrix</param>
    /// <returns>Subtraction result</returns>
    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Subtract((Matrix)m2);

    static Matrix IGroup<Matrix>.operator -(in Matrix first, in Matrix second) => first.Subtract(in second);

    public static Matrix operator -(Scalar f, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => ScaleMatrix(m._columns, m._rows, f).Subtract((Matrix)m);

    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Subtract(f);

    static Matrix IRing<Matrix>.operator *(in Matrix first, in Matrix second) => first.Multiply(in second);

    static Matrix Algebra<Scalar>.IVectorSpace<Matrix>.operator *(Scalar scalar, in Matrix matrix) => matrix.Multiply(scalar);

    static Matrix Algebra<Scalar>.IVectorSpace<Matrix>.operator *(in Matrix matrix, Scalar scalar) => matrix.Multiply(scalar);

    public static Vector operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> matrix, in Vector v) => matrix.Multiply(v);

    public static Matrix operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Multiply((Matrix)m2);

    public static Matrix operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> matrix, Scalar scalar) => matrix.Multiply(scalar);

    public static Matrix operator *(Scalar scalar, in MatrixNM<Vector, Matrix, Polynomial, Scalar> matrix) => matrix.Multiply(scalar);

    public static MatrixNM<Vector, Matrix, Polynomial, Scalar> operator ^(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, int c) => m.Power(c);

    public static Matrix operator /(in MatrixNM<Vector, Matrix, Polynomial, Scalar> matrix, Scalar scalar) => matrix.Divide(scalar);

    static Matrix Algebra<Scalar>.IVectorSpace<Matrix>.operator /(in Matrix matrix, Scalar scalar) => matrix.Divide(scalar);

    public static Matrix operator %(in MatrixNM<Vector, Matrix, Polynomial, Scalar> matrix, Scalar scalar) => matrix.Modulus(scalar);

    static Matrix Algebra<Scalar>.IVectorSpace<Matrix>.operator %(in Matrix matrix, Scalar scalar) => matrix.Modulus(scalar);

    public static implicit operator Matrix(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => FromCoefficients(m.Coefficients);

    public static explicit operator Vector[](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Columns;

    public static explicit operator Scalar[](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.ToArray();

    public static implicit operator Scalar[,](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Coefficients;

    public static implicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(in Scalar[,] arr) => FromCoefficients(arr);

    public static explicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(Scalar s) => ScaleMatrix(1, 1, s);

    public static implicit operator CompressedStorageFormat<Scalar>(MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.ToCompressedStorageFormat();

    public static implicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(CompressedStorageFormat<Scalar> c) => FromCompressedStorageFormat(c);

    //public static bool operator ==(Matrix? first, Matrix? second) => first?.Is(second) ?? second is null;

    //public static bool operator !=(Matrix? first, Matrix? second) => !(first == second);

    #endregion
}

/// <summary>
/// Represents an abstract vector space.
/// <para/>
/// NOTE: All classes inheriting from this type must provide a constructor accepting an <see cref="IEnumerable{}"/> of <typeparamref name="Vector"/>.
/// </summary>
/// <typeparam name="Space">The vector space type.</typeparam>
/// <typeparam name="Vector">The underlying vector type.</typeparam>
/// <typeparam name="Scalar">The underlying scalar value type.</typeparam>
public abstract class VectorSpace<Space, Vector, Scalar>
    where Space : VectorSpace<Space, Vector, Scalar>
    where Vector : Algebra<Scalar>.IVector<Vector>
    where Scalar : unmanaged, IField<Scalar>
{
    #region PROPERTIES / FIELDS

    private static readonly Func<IEnumerable<Vector>, Space> _create;
    private protected readonly List<Vector> _basis = [];


    public Vector[] Basis => _basis.ToArray();

    public abstract Vector this[params Scalar[] coefficients] { get; }

    public Space Normalized => FromVectors(_basis.Select(v => v.Normalized));

    public int Dimension => _basis.Count;

    public bool IsEmpty => Dimension == 0;

    #endregion
    #region CONSTRUCTORS

    static VectorSpace()
    {
        Type space = typeof(Space);

        if (space.GetConstructor([typeof(IEnumerable<Vector>)]) is { } c)
            _create = v => (Space)c.Invoke(new object[] { v });
        else
            throw new InvalidOperationException($"The type '{space}' cannot be used as vector space type as it does not provide a constructor accepting a single parameter of the type '{typeof(IEnumerable<Vector>)}'.");
    }

    public VectorSpace(IEnumerable<Vector> basis) => _basis.AddRange(GetLinearIndependantSet(basis.Where(b => b.IsNonZero)));

    public VectorSpace(params Vector[] basis)
        : this(basis as IEnumerable<Vector>)
    {
    }

    public static Vector[] GetLinearIndependantSet(IEnumerable<Vector> vectors)
    {
        List<Vector> result = [];

        foreach (Vector v in vectors)
            if (v.IsNonZero && result.All(b => !v.IsLinearDependant(b, out _)))
                result.Add(v);

        return [.. result];
    }

    #endregion
    #region INSTANCE METHODS

    public Space Add(Space second) => FromVectors(_basis.Concat(second._basis));

    public Space Divide(Space second) => FromVectors(_basis.Where(v => second._basis.All(b => !v.IsLinearDependant(b, out _))));

    public bool Contains(Vector vector) => Contains(vector, out _);

    public abstract bool Contains(Vector vector, out Vector coefficients);

    #endregion
    #region STATIC METHODS

    public static Space FromVectors(IEnumerable<Vector> vectors) => _create(vectors);

    #endregion
    #region OPERATORS

    public static implicit operator VectorSpace<Space, Vector, Scalar>(Vector[] vectors) => FromVectors(vectors);

    public static implicit operator Space(VectorSpace<Space, Vector, Scalar> space) => _create(space._basis);

    #endregion
}

public static class VectorSpaceExtensions
{
    public static Space ToVectorSpace<Space, Vector, Scalar>(this IEnumerable<Vector> vectors)
        where Space : VectorSpace<Space, Vector, Scalar>
        where Vector : Algebra<Scalar>.IVector<Vector>
        where Scalar : unmanaged, IField<Scalar>
        => VectorSpace<Space, Vector, Scalar>.FromVectors(vectors);
}

public abstract class WritableMatrixNM<Vector, Matrix, Polynomial, Scalar>
    : MatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Vector : WritableVectorN<Vector, Matrix, Polynomial, Scalar>
    where Matrix : WritableMatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    public override Scalar[] FlattenedCoefficients => _coefficients;


    public WritableMatrixNM(params Vector[] columns)
        : base(columns)
    {
    }

    public WritableMatrixNM(IEnumerable<Vector> columns)
        : base(columns)
    {
    }

    public WritableMatrixNM(Scalar[,] values)
        : base(values)
    {
    }

    public WritableMatrixNM(Matrix matrix)
        : base(matrix)
    {
    }

    public WritableMatrixNM(int columns, int rows)
        : base(columns, rows)
    {
    }

    public WritableMatrixNM(int columns, int rows, Scalar scale)
        : base(columns, rows, scale)
    {
    }

    public WritableMatrixNM(int columns, int rows, IEnumerable<Scalar>? values)
        : base(columns, rows, values)
    {
    }

    public WritableMatrixNM(int columns, int rows, Scalar[,] values)
        : base(columns, rows, values)
    {
    }

    public override Matrix SetValue(int row, int column, Scalar value)
    {
        if (row >= _rows || column >= _columns || row < 0 || column < 0)
            throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).");

        _coefficients[row * _columns + column] = value;

        return this;
    }

    public override Matrix SetRegion(Range columns, Range rows, in Matrix values)
    {
        int[] idx_c = columns.GetOffsets(_columns);
        int[] idx_r = rows.GetOffsets(_rows);
        Scalar[] m = values._coefficients;

        for (int i = 0; i < idx_c.Length; ++i)
            for (int j = 0; j < idx_r.Length; ++j)
                _coefficients[idx_r[j] * _columns + idx_c[i]] = m[j * values._columns + i];

        return this;
    }

    public override Matrix SetColumn(int column, in Vector vector)
    {
        Scalar[] m = vector.Coefficients;

        for (int r = 0; r < _rows; ++r)
            _coefficients[r * _columns + column] = m[r];

        return this;
    }

    public override Matrix SetRow(int row, in Vector vector)
    {
        Scalar[] m = vector.Coefficients;

        for (int c = 0; c < _columns; ++c)
            _coefficients[row * _columns + c] = m[c];

        return this;
    }
}

public abstract class WritableVectorN<Vector, Matrix, Polynomial, Scalar>
    : VectorN<Vector, Matrix, Polynomial, Scalar>
    where Vector : WritableVectorN<Vector, Matrix, Polynomial, Scalar>
    where Matrix : WritableMatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    public override Scalar[] Coefficients => _coefficients;


    public WritableVectorN(in Vector vector)
        : base(vector)
    {
    }

    public WritableVectorN(params Scalar[] coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(IEnumerable<Scalar>? coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(int size, Scalar value)
        : base(size, value)
    {
    }

    public override Vector SetEntry(int index, Scalar value)
    {
        if (index < 0 || index > _coefficients.Length)
            throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.");

        _coefficients[index] = value;

        return this;
    }

    public override Vector SetEntries(Range rows, Vector values)
    {
        // TODO : range checks

        int[] idxs = rows.GetOffsets(Size);
        Scalar[] v = values;

        for (int i = 0; i < idxs.Length; ++i)
            _coefficients[idxs[i]] = v[i];

        return this;
    }

    public override Vector SwapEntries(int src_idx, int dst_idx)
    {
        // TODO : range checks

        Scalar tmp = _coefficients[src_idx];

        _coefficients[src_idx] = _coefficients[dst_idx];
        _coefficients[dst_idx] = tmp;

        return this;
    }
}

public class VectorN<T>
    : VectorN<VectorN<T>, MatrixNM<T>, Polynomial<T>, Scalar<T>>
    , IVectorN<VectorN<T>, Scalar<T>>
    where T : unmanaged, IComparable<T>
{
    public VectorN(in VectorN<T> vector)
        : base(vector)
    {
    }

    public VectorN(params Scalar<T>[] coefficients)
        : base(coefficients)
    {
    }

    public VectorN(IEnumerable<Scalar<T>>? coefficients)
        : base(coefficients)
    {
    }

    public VectorN(int size, Scalar<T> value)
        : base(size, value)
    {
    }


    public static VectorN<T> FromCoefficients(params T[] coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static VectorN<T> FromCoefficients(IEnumerable<T> coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static new VectorN<T> FromCoefficients(params Scalar<T>[] coefficients) => new(coefficients);

    public static new VectorN<T> FromCoefficients(IEnumerable<Scalar<T>> coefficients) => new(coefficients);
}

public class WritableVectorN<T>
    : WritableVectorN<WritableVectorN<T>, WritableMatrixNM<T>, Polynomial<T>, Scalar<T>>
    , IVectorN<WritableVectorN<T>, Scalar<T>>
    where T : unmanaged, IComparable<T>
{
    public WritableVectorN(in WritableVectorN<T> vector)
        : base(vector)
    {
    }

    public WritableVectorN(params Scalar<T>[] coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(IEnumerable<Scalar<T>>? coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(int size, Scalar<T> value)
        : base(size, value)
    {
    }


    public static WritableVectorN<T> FromCoefficients(params T[] coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static WritableVectorN<T> FromCoefficients(IEnumerable<T> coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static new WritableVectorN<T> FromCoefficients(params Scalar<T>[] coefficients) => new(coefficients);

    public static new WritableVectorN<T> FromCoefficients(IEnumerable<Scalar<T>> coefficients) => new(coefficients);

    public static implicit operator WritableVectorN<T>(VectorN<T> vec) => new(vec.Coefficients);

    public static implicit operator VectorN<T>(WritableVectorN<T> vec) => new(vec.Coefficients);
}

public partial class VectorN
    : VectorN<VectorN, MatrixNM, Polynomial, Scalar>
    , IVectorN<VectorN, Scalar>
{
    public VectorN(in VectorN vector)
        : base(vector)
    {
    }

    public VectorN(params Scalar[] coefficients)
        : base(coefficients)
    {
    }

    public VectorN(IEnumerable<Scalar>? coefficients)
        : base(coefficients)
    {
    }

    public VectorN(int size, Scalar value)
        : base(size, value)
    {
    }


    public static new VectorN FromCoefficients(params Scalar[] coefficients) => new(coefficients);

    public static new VectorN FromCoefficients(IEnumerable<Scalar> coefficients) => new(coefficients);


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator num.Vector<Scalar>(<#=typename#> v) => new num.Vector<Scalar>(v.Coefficients.Cast<Scalar>().ToArray());

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator num.Vector<double>(<#=typename#> v) => new num.Vector<double>(v.Coefficients.Cast<double>().ToArray());

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator num.Vector<float>(<#=typename#> v) => new num.Vector<float>(v.Coefficients.Cast<float>().ToArray());

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator num.Vector<long>(<#=typename#> v) => new num.Vector<long>(v.Coefficients.Cast<long>().ToArray());

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator num.Vector<int>(<#=typename#> v) => new num.Vector<int>(v.Coefficients.Cast<int>().ToArray());

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator <#=typename#>(num.Vector<Scalar> v) => new <#=typename#>(<#=string.Join(", ", r(0, dim).Select(i => $"v[{i}]"))#>);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator <#=typename#>(num.Vector<double> v) => new <#=typename#>(<#=string.Join(", ", r(0, dim).Select(i => $"v[{i}]"))#>);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator <#=typename#>(num.Vector<float> v) => new <#=typename#>(<#=string.Join(", ", r(0, dim).Select(i => $"v[{i}]"))#>);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator <#=typename#>(num.Vector<long> v) => new <#=typename#>(<#=string.Join(", ", r(0, dim).Select(i => $"v[{i}]"))#>);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static implicit operator <#=typename#>(num.Vector<int> v) => new <#=typename#>(<#=string.Join(", ", r(0, dim).Select(i => $"v[{i}]"))#>);

}

public class ComplexVectorN
    : VectorN<ComplexVectorN, ComplexMatrixNM, ComplexPolynomial, Complex>
    , IVectorN<ComplexVectorN, Complex>
{
    public ComplexVectorN Conjugate => FromArray(Coefficients.Select(c => c.Conjugate).ToArray());


    public ComplexVectorN(in ComplexVectorN vector)
        : base(vector)
    {
    }

    public ComplexVectorN(params Complex[] coefficients)
        : base(coefficients)
    {
    }

    public ComplexVectorN(IEnumerable<Complex>? coefficients)
        : base(coefficients)
    {
    }

    public ComplexVectorN(int size, Complex value)
        : base(size, value)
    {
    }


    public static new ComplexVectorN FromCoefficients(params Complex[] coefficients) => new(coefficients);

    public static new ComplexVectorN FromCoefficients(IEnumerable<Complex> coefficients) => new(coefficients);
}

public class MatrixNM<T>
    : MatrixNM<VectorN<T>, MatrixNM<T>, Polynomial<T>, Scalar<T>>
    , IMatrixNM<VectorN<T>, MatrixNM<T>, Scalar<T>>
    where T : unmanaged, IComparable<T>
{
    public MatrixNM(params VectorN<T>[] columns)
        : base(columns)
    {
    }

    public MatrixNM(IEnumerable<VectorN<T>> columns)
        : base(columns)
    {
    }

    public MatrixNM(Scalar<T>[,] values)
        : base(values)
    {
    }

    public MatrixNM(MatrixNM<T> matrix)
        : base(matrix)
    {
    }

    public MatrixNM(int columns, int rows)
        : base(columns, rows)
    {
    }

    public MatrixNM(int columns, int rows, Scalar<T> scale)
        : base(columns, rows, scale)
    {
    }

    public MatrixNM(int columns, int rows, IEnumerable<Scalar<T>>? values)
        : base(columns, rows, values)
    {
    }

    public MatrixNM(int columns, int rows, Scalar<T>[] values)
        : base(columns, rows, values)
    {
    }

    protected override (VectorN<T> Eigenvector, Scalar<T> Eigenvalue) DoInverseVectoriteration(Scalar<T> offset, IEqualityComparer<Scalar<T>> comparer) => throw new NotImplementedException();


    public static MatrixNM<T> FromCoefficients(int columns, int rows, T[] coefficients) => FromCoefficients(columns, rows, coefficients.ToArray(c => new Scalar<T>(c)));

    public static MatrixNM<T> FromCoefficients(T[,] coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static new MatrixNM<T> FromCoefficients(int columns, int rows, Scalar<T>[] coefficients) => new(columns, rows, coefficients);

    public static new MatrixNM<T> FromCoefficients(Scalar<T>[,] coefficients) => new(coefficients);

    public static new MatrixNM<T> FromColumnVectors(VectorN<T>[] columns) => new(columns);
}

public class WritableMatrixNM<T>
    : WritableMatrixNM<WritableVectorN<T>, WritableMatrixNM<T>, Polynomial<T>, Scalar<T>>
    , IMatrixNM<WritableVectorN<T>, WritableMatrixNM<T>, Scalar<T>>
    where T : unmanaged, IComparable<T>
{
    public WritableMatrixNM(params WritableVectorN<T>[] columns)
        : base(columns)
    {
    }

    public WritableMatrixNM(IEnumerable<WritableVectorN<T>> columns)
        : base(columns)
    {
    }

    public WritableMatrixNM(Scalar<T>[,] values)
        : base(values)
    {
    }

    public WritableMatrixNM(WritableMatrixNM<T> matrix)
        : base(matrix)
    {
    }

    public WritableMatrixNM(int columns, int rows)
        : base(columns, rows)
    {
    }

    public WritableMatrixNM(int columns, int rows, Scalar<T> scale)
        : base(columns, rows, scale)
    {
    }

    public WritableMatrixNM(int columns, int rows, IEnumerable<Scalar<T>>? values)
        : base(columns, rows, values)
    {
    }

    public WritableMatrixNM(int columns, int rows, Scalar<T>[] values)
        : base(columns, rows, values)
    {
    }

    protected override (WritableVectorN<T> Eigenvector, Scalar<T> Eigenvalue) DoInverseVectoriteration(Scalar<T> offset, IEqualityComparer<Scalar<T>> comparer) => throw new NotImplementedException();


    public static WritableMatrixNM<T> FromCoefficients(int columns, int rows, T[] coefficients) => FromCoefficients(columns, rows, coefficients.ToArray(c => new Scalar<T>(c)));

    public static WritableMatrixNM<T> FromCoefficients(T[,] coefficients) => FromCoefficients(coefficients.Select(c => new Scalar<T>(c)));

    public static new WritableMatrixNM<T> FromCoefficients(int columns, int rows, Scalar<T>[] coefficients) => new(columns, rows, coefficients);

    public static new WritableMatrixNM<T> FromCoefficients(Scalar<T>[,] coefficients) => new(coefficients);

    public static new WritableMatrixNM<T> FromColumnVectors(WritableVectorN<T>[] columns) => new(columns);

    public static implicit operator WritableMatrixNM<T>(MatrixNM<T> mat) => new(mat.Coefficients);

    public static implicit operator MatrixNM<T>(WritableMatrixNM<T> mat) => new(mat.Coefficients);
}

// TODO : cast matrix to complexmatrix

public partial class MatrixNM
    : MatrixNM<VectorN, MatrixNM, Polynomial, Scalar>
    , IMatrixNM<VectorN, MatrixNM, Scalar>
{
    public MatrixNM(params VectorN[] columns)
        : base(columns)
    {
    }

    public MatrixNM(IEnumerable<VectorN> columns)
        : base(columns)
    {
    }

    public MatrixNM(Scalar[,] values)
        : base(values)
    {
    }

    public MatrixNM(MatrixNM matrix)
        : base(matrix)
    {
    }

    public MatrixNM(int columns, int rows)
        : base(columns, rows)
    {
    }

    public MatrixNM(int columns, int rows, Scalar scale)
        : base(columns, rows, scale)
    {
    }

    public MatrixNM(int columns, int rows, IEnumerable<Scalar>? values)
        : base(columns, rows, values)
    {
    }

    public MatrixNM(int columns, int rows, Scalar[] values)
        : base(columns, rows, values)
    {
    }

    protected override (VectorN Eigenvector, Scalar Eigenvalue) DoInverseVectoriteration(Scalar offset, IEqualityComparer<Scalar> comparer)
    {
        int dimension = Size.Columns;
        VectorN v_old = new(Enumerable.Repeat(Scalar.Zero, dimension));
        VectorN v_new = new(Enumerable.Repeat(Scalar.Random(), dimension));
        VectorN v_init = v_new;
        VectorN w = v_old;
        MatrixNM A = (this - (offset * IdentityMatrix(dimension))).Inverse;

        while (comparer.Equals((v_old - v_new).Length, Scalar.ComputationalEpsilon))
            (v_old, v_new) = (v_new, A* v_new);

        v_new = ~v_new;
        v_old = ~v_old;

        while ((w* v_init).IsZero)
            w = new VectorN(Enumerable.Repeat(Scalar.Random(), dimension));

        Scalar λ = (v_new * w) / (v_old * w);

        return (v_new, 1 / (λ - offset));
    }

    public (MatrixNM Q, MatrixNM R) QRDecompose() => throw new NotImplementedException(); // TODO

    public static new MatrixNM FromCoefficients(int columns, int rows, Scalar[] coefficients) => new(columns, rows, coefficients);

    public static new MatrixNM FromCoefficients(Scalar[,] coefficients) => new(coefficients);

    public static new MatrixNM FromColumnVectors(VectorN[] columns) => new(columns);
}

public class ComplexMatrixNM
    : MatrixNM<ComplexVectorN, ComplexMatrixNM, ComplexPolynomial, Complex>
    , IMatrixNM<ComplexVectorN, ComplexMatrixNM, Complex>
{
    public ComplexMatrixNM Conjugate => FromCoefficients(ColumnCount, RowCount, FlattenedCoefficients.Select(c => c.Conjugate).ToArray());

    public ComplexMatrixNM ConjugateTranspose => Conjugate.Transposed;

    public bool IsUnitary => Multiply(ConjugateTranspose).IsIdentity;

    public bool IsHermitian => Is(ConjugateTranspose);

    public bool IsSelfAdjoint => IsHermitian;


    public ComplexMatrixNM(params ComplexVectorN[] columns)
        : base(columns)
    {
    }

    public ComplexMatrixNM(IEnumerable<ComplexVectorN> columns)
        : base(columns)
    {
    }

    public ComplexMatrixNM(Complex[,] values)
        : base(values)
    {
    }

    public ComplexMatrixNM(ComplexMatrixNM matrix)
        : base(matrix)
    {
    }

    public ComplexMatrixNM(int columns, int rows)
        : base(columns, rows)
    {
    }

    public ComplexMatrixNM(int columns, int rows, Complex scale)
        : base(columns, rows, scale)
    {
    }

    public ComplexMatrixNM(int columns, int rows, IEnumerable<Complex>? values)
        : base(columns, rows, values)
    {
    }

    public ComplexMatrixNM(int columns, int rows, Complex[] values)
        : base(columns, rows, values)
    {
    }

    protected override (ComplexVectorN Eigenvector, Complex Eigenvalue) DoInverseVectoriteration(Complex offset, IEqualityComparer<Complex> comparer)
    {
        int dimension = Size.Columns;
        ComplexVectorN v_old = new(Enumerable.Repeat(Complex.Zero, dimension));
        ComplexVectorN v_new = new(Enumerable.Repeat(Complex.Random(), dimension));
        ComplexVectorN v_init = v_new;
        ComplexVectorN w = v_old;
        ComplexMatrixNM A = (this - (offset * IdentityMatrix(dimension))).Inverse;

        while (comparer.Equals((v_old - v_new).Length, Complex.Zero))
            (v_old, v_new) = (v_new, A * v_new);

        v_new = ~v_new;
        v_old = ~v_old;

        while ((w * v_init).IsZero)
            w = new ComplexVectorN(Enumerable.Repeat(Complex.Random(), dimension));

        Complex λ = (v_new * w) / (v_old * w);

        return (v_new, 1 / (λ - offset));
    }

    public static new ComplexMatrixNM FromCoefficients(int columns, int rows, Complex[] coefficients) => new(columns, rows, coefficients);

    public static new ComplexMatrixNM FromCoefficients(Complex[,] coefficients) => new(coefficients);

    public static new ComplexMatrixNM FromColumnVectors(ComplexVectorN[] columns) => new(columns);
}
