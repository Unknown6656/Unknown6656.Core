#nullable enable

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

        List<Field> vals = new List<Field>();
        List<int> rows = new List<int>();
        List<int> cols = new List<int>();

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

        Values = vals.ToArray();
        Rows = rows.ToArray();
        Cols = cols.ToArray();
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
        List<int> cols = Cols.ToList();

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

    public static CompressedStorageFormat<Field> FromBytes(byte[] bytes) => new CompressedStorageFormat<Field>(bytes);

    public static CompressedStorageFormat<Field> FromMatrix(Field[,] matrix) => new CompressedStorageFormat<Field>(matrix);

    public static CompressedStorageFormat<Field> FromMatrix<T>(T matrix) where T : Algebra<Field>.IComposite2D => new CompressedStorageFormat<Field>(matrix);

    public static implicit operator byte[](CompressedStorageFormat<Field> compressed) => compressed.ToBytes();

    public static implicit operator CompressedStorageFormat<Field>(byte[] bytes) => FromBytes(bytes);
}

internal static class Constructor<Vector, Matrix, Polynomial, Scalar>
    where Vector : VectorN<Vector, Matrix, Polynomial, Scalar>
    where Matrix : MatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    private static readonly Func<IEnumerable<Scalar>, Polynomial> _create_polynomial;
    private static readonly Func<IEnumerable<Scalar>, Vector> _create_vector;
    private static readonly Func<Scalar[,], Matrix> _create_matrix;

    public static Scalar ScalarZero { get; }
    public static Scalar ScalarOne { get; }
    public static Scalar ScalarTwo { get; }
    public static Scalar ScalarNegativeOne { get; }
    public static IEqualityComparer<Scalar> ScalarEqualityComparer { get; }


    static Constructor()
    {
        Type V = typeof(Vector);
        Type M = typeof(Matrix);
        Type P = typeof(Polynomial);
        Type T = typeof(Scalar);

        if (P.GetConstructor(new[] { typeof(Scalar[]) }) is { } ctor1)
            _create_polynomial = c => (Polynomial)ctor1.Invoke(new object[] { c is Scalar[] a ? a : c.ToArray() });
        else
            throw new InvalidOperationException($"The type parameter '{P}' cannot be used as polynomial function type, as it has no constructor accepting an array of polynomial coefficents ('{typeof(Scalar[])}').");

        if (V.GetConstructor(new[] { typeof(Scalar[]) }) is { } ctor2)
            _create_vector = c => (Vector)ctor2.Invoke(new object[] { c is Scalar[] a ? a : c.ToArray() });
        else
            throw new InvalidOperationException($"The type parameter '{V}' cannot be used as vector type, as it has no constructor accepting an array of scalar coefficents ('{typeof(Scalar[])}').");

        if (M.GetConstructor(new[] { typeof(Scalar[,]) }) is { } ctor3)
            _create_matrix = c => (Matrix)ctor3.Invoke(new object[] { c });
        else
            throw new InvalidOperationException($"The type parameter '{M}' cannot be used as matrix type, as it has no constructor accepting an two-dimensional array of scalar coefficents ('{typeof(Scalar[,])}').");

        ScalarZero = default;
        ScalarOne = ScalarZero.Increment();
        ScalarTwo = ScalarOne.Increment();
        ScalarNegativeOne = ScalarOne.Negate();
        ScalarEqualityComparer = EqualityComparer<Scalar>.Default;

        if (typeof(Scalar).GetProperty("EqualityComparer", BindingFlags.Public | BindingFlags.Static) is { } pinfo && pinfo.GetValue(null) is IEqualityComparer<Scalar> c)
            ScalarEqualityComparer = c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Polynomial CreatePolynomial(params Scalar[] coefficients) => _create_polynomial(coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Polynomial CreatePolynomial(IEnumerable<Scalar> coefficients) => _create_polynomial(coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector CreateVector(params Scalar[] coefficients) => _create_vector(coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector CreateVector(IEnumerable<Scalar> coefficients) => _create_vector(coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix CreateMatrix(Vector[] columns)
    {
        Scalar[,] coeff = new Scalar[columns.Length, columns.Max(c => c.Size)];

        for (int c = 0; c < columns.Length; ++c)
            for (int r = 0, s = columns[c].Size; r < s; ++r)
                coeff[c, r] = columns[c][r];

        return CreateMatrix(coeff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix CreateMatrix(Scalar[,] coefficients) => _create_matrix(coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix CreateMatrix(int columns, int rows, Scalar[] coefficients)
    {
        Scalar[,] coeff = new Scalar[columns, rows];

        for (int i = 0; i < coefficients.Length; ++i)
            coeff[i % columns, i / columns] = coefficients[i];

        return CreateMatrix(coeff);
    }
}

public unsafe abstract class VectorN<Vector, Matrix, Polynomial, Scalar>
    : Algebra<Scalar>.IVector<Vector, Matrix>
    , Algebra<Scalar, Polynomial>.IComposite1D
    , IEnumerable<Scalar>
    , IComparable<Vector>
    , IComparable
    , ICloneable
    where Vector : VectorN<Vector, Matrix, Polynomial, Scalar>
    where Matrix : MatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>
    where Scalar : unmanaged, IScalar<Scalar>, IComparable<Scalar>
{
    #region PRIVATE & STATIC FIELDS

#pragma warning disable IDE0032
    protected readonly Scalar[] _coefficients;
#pragma warning restore IDE0032

    #endregion
    #region INDEXERS

    public Scalar this[int index] => GetEntry(index);

    public Vector this[int index, Scalar value] => SetEntry(index, value);

    public Scalar this[Index index] => GetEntry(index.GetOffset(_coefficients.Length));

    public Vector this[Range rows] => GetEntries(rows);

    public Vector this[Range rows, in Vector values] => SetEntries(rows, values);

    #endregion
    #region INSTANCE PROPERTIES

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

    public Scalar Sum => _coefficients.Sum();

    public Scalar Avg => _coefficients.Average();

    public Scalar Min => _coefficients.Min();

    public Scalar Max => _coefficients.Max();

    public Scalar Length => SquaredNorm.Sqrt();

    [DebuggerHidden, DebuggerNonUserCode, DebuggerBrowsable(DebuggerBrowsableState.Never), EditorBrowsable(EditorBrowsableState.Never)]
    public Scalar SquaredNorm => _coefficients.Select(c => c.Multiply(c)).Sum();

    public Scalar SquaredLength => SquaredNorm;

    public Vector Normalized => Length is Scalar l && l.IsZero ? ZeroVector(Size) : Divide(l);

    public bool IsNormalized => Length.IsOne;

    public bool IsInsideUnitSphere => Length.CompareTo(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne) <= 0;

    public Matrix AsMatrix => MatrixNM<Vector, Matrix, Polynomial, Scalar>.DiagonalMatrix(this);

    public Matrix HouseholderMatrix => IsZero ? throw new InvalidOperationException("The Householder matrix is undefined for zero vectors.")
                                              : OuterProduct(this).Multiply(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarTwo).Divide(SquaredNorm);

    public Matrix Transposed => MatrixNM<Vector, Matrix, Polynomial, Scalar>.FromRows(new Vector[] { this });

    #endregion
    #region CONSTRUCTORS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorN(in Vector vector)
        : this(vector._coefficients)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorN(int size, Scalar value)
        : this(Enumerable.Repeat(value, size))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorN(params Scalar[]? coefficients)
        : this(coefficients as IEnumerable<Scalar>)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorN(IEnumerable<Scalar>? coefficients) => _coefficients = coefficients?.ToArray() ?? Array.Empty<Scalar>();

    #endregion
    #region INSTANCE FUNCTIONS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Negate() => FromCollection(_coefficients.Select(c => c.Negate()));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Add(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                               : FromCollection(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Add(y)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Add(params Vector[] others) => others.Aggregate((Vector)this, (t, n) => t.Add(n));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Subtract(in Vector second) => Add(second.AdditiveInverse);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Subtract(params Vector[] others) => others.Aggregate((Vector)this, (t, n) => t.Subtract(n));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Increment() => Add(ScalarVector(Size, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Decrement() => Subtract(ScalarVector(Size, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Multiply(Scalar factor) => FromCollection(_coefficients.Select(c => c.Multiply(factor)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Multiply(params Scalar[] factors) => factors.Aggregate((Vector)this, (t, n) => t.Multiply(n));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Divide(Scalar factor) => Multiply(factor.MultiplicativeInverse);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Divide(params Scalar[] factors) => factors.Aggregate((Vector)this, (t, n) => t.Divide(n));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ComponentwiseDivide(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                                               : FromCollection(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Divide(y)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ComponentwiseMultiply(in Vector second) => Size != second.Size ? throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(second))
                                                                                 : FromCollection(_coefficients.ZipOuter(second._coefficients, (x, y) => x.Multiply(y)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ComponentwiseAbsolute() => FromCollection(_coefficients.Select(c => c.Abs()));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ComponentwisSqrt() => FromCollection(_coefficients.Select(c => c.Sqrt()));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Power(int e)
    {
        if (e < 0)
            throw new ArgumentOutOfRangeException(nameof(e));

        Vector r = ScalarVector(Size, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar Dot(in Vector other)
    {
        if (Size != other.Size)
            throw new ArgumentException("Mismatching dimensions: Both vectors must have the same dimensions.", nameof(other));

        Scalar acc = default;

        for (int i = 0; i < _coefficients.Length; ++i)
            acc = _coefficients[i].Multiply(other._coefficients[i]).Add(acc);

        return acc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOrthogonal(in Vector second) => Dot(second).IsZero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Reflect(in Vector normal)
    {
        Scalar θ = Dot(normal);

        return normal.Multiply(θ.Add(θ)).Subtract(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar DistanceTo(in Vector second) => Subtract(second).Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Refract(in Vector normal, Scalar eta, out Vector refracted)
    {
        Scalar one = Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne;
        Scalar θ = Dot(normal);
        Scalar k = one.Subtract(eta.Multiply(eta, one.Subtract(θ.Multiply(θ))));
        bool res = k.IsNegative;

        refracted = res ? Reflect(normal.Negate()) : Multiply(eta).Add(normal.Multiply(eta.Multiply(θ).Subtract(k.Sqrt())));

        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar AngleTo(in Vector second) => Dot(second).Acos();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix OuterProduct(in Vector second)
    {
        Scalar[,] coeff = new Scalar[Size, Size];

        for (int r = 0; r < Size; ++r)
            for (int c = 0; c < Size; ++c)
                coeff[c, r] = second._coefficients[r].Multiply(second._coefficients[c]);

        return Constructor<Vector, Matrix, Polynomial, Scalar>.CreateMatrix(coeff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar GetEntry(int index) => index < 0 || index > _coefficients.Length ? throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.")
                                                                              : _coefficients[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Vector SetEntry(int index, Scalar value) => index < 0 || index > _coefficients.Length ? throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.")
                                                                                       : Constructor<Vector, Matrix, Polynomial, Scalar>.CreateVector(_coefficients.Take(index).Append(value).Concat(_coefficients.Skip(index + 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetEntries(Range rows) => FromArray(_coefficients[rows]); // TODO : range checks

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Vector SwapEntries(int src_idx, int dst_idx)
    {
        // TODO : range checks

        Scalar[] t = Coefficients;
        Scalar tmp = t[src_idx];

        t[src_idx] = t[dst_idx];
        t[dst_idx] = tmp;

        return FromArray(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetMinor(int row) => FromCollection(_coefficients.Take(row).Concat(_coefficients.Skip(row + 1)));

    /// <inheritdoc cref="IVector{V, S}.Clamp(S, S)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Clamp() => Clamp(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Clamp(Scalar low, Scalar high) => FromCollection(_coefficients.Select(c => c.Clamp(low, high)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector NormalizeMinMax()
    {
        Scalar min = Min;
        Scalar fac = Max.Subtract(min);

        return FromCollection(_coefficients.Select(c => c.Subtract(min).Divide(fac)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector LinearInterpolate(in Vector other, Scalar factor) => Multiply(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne.Subtract(factor)).Add(other.Multiply(factor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Vector? other) => other is null ? 1 : Length.CompareTo(other.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? other) => other is { } ? CompareTo((Vector)other) : throw new ArgumentNullException(nameof(other));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Is(Vector? other) => other is { } && _coefficients.Are(other._coefficients, EqualityComparer<Scalar>.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Is(Vector other, Scalar tolerance) => Size == other.Size && _coefficients.Zip(other._coefficients, (c1, c2) => c1.Subtract(c2).Abs().CompareTo(tolerance)).All(c => c <= 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNot(Vector other) => !Is(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => LINQ.GetHashCode(_coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Vector v && Equals(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector? other) => Is(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => $"({string.Join(", ", _coefficients)})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<Scalar> GetEnumerator() => _coefficients.Cast<Scalar>().GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => _coefficients.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Clone() => FromArray(_coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] ToArray() => Coefficients;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public X[] ToArray<X>() where X : unmanaged => _coefficients.CopyTo<Scalar, X>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToNative<X>(X* dst) where X : unmanaged => _coefficients.CopyTo(dst);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Polynomial ToPolynomial() => Constructor<Vector, Matrix, Polynomial, Scalar>.CreatePolynomial(_coefficients);

    #endregion
    #region STATIC FUNCTIONS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector ZeroVector(int size) => ScalarVector(size, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector ScalarVector(int size, Scalar s) => FromCollection(Enumerable.Repeat(s, size));

    /// <inheritdoc cref="Algebra{S}.IEucledianVectorSpace{V}.Dot(V)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Scalar Dot(in Vector v1, in Vector v2) => v1.Dot(v2);

    /// <inheritdoc cref="AngleTo"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Scalar AngleBetween(in Vector v1, in Vector v2) => v1.AngleTo(v2);

    /// <inheritdoc cref="Algebra{S}.IVectorSpace{V}.IsLinearDependant(V, out S?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLinearDependant(in Vector v1, in Vector v2) => v1.IsLinearDependant(v2, out _);

    /// <inheritdoc cref="Algebra{S}.IVector{V, M}.OuterProduct"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix OuterProduct(in Vector v1, in Vector v2) => v1.OuterProduct(v2);

    /// <inheritdoc cref="Algebra{S}.IVectorSpace{V}.LinearInterpolate(V, S)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector LinearInterpolate(in Vector v1, in Vector v2, Scalar factor) => v1.LinearInterpolate(v2, factor);

    /// <inheritdoc cref="IsLinearDependant(Vector, out Scalar?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLinearDependant(in Vector v1, in Vector v2, out Scalar? factor) => v1.IsLinearDependant(v2, out factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector FromArray(params Scalar[] v) => Constructor<Vector, Matrix, Polynomial, Scalar>.CreateVector(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector FromCollection(IEnumerable<Scalar> v) => Constructor<Vector, Matrix, Polynomial, Scalar>.CreateVector(v);

    #endregion
    #region OPERATORS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Is(v2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.IsNot(v2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.CompareTo(v2) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1 == v2 || v1 < v2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.CompareTo(v2) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1 == v2 || v1 > v2;

    /// <summary>
    /// Normalizes the given vector
    /// </summary>
    /// <param name="v">Original vector</param>
    /// <returns>Normalized vector</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator ~(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Normalized;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator +(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator -(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Negate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator ++(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Increment();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator --(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Decrement();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator +(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Add(v2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator -(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Subtract(v2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Scalar operator *(in VectorN<Vector, Matrix, Polynomial, Scalar> v1, in VectorN<Vector, Matrix, Polynomial, Scalar> v2) => v1.Dot(v2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(Scalar f, in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Multiply(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(in VectorN<Vector, Matrix, Polynomial, Scalar> v, Scalar f) => v.Multiply(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator /(in VectorN<Vector, Matrix, Polynomial, Scalar> v, Scalar f) => v.Divide(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Scalar[](in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.Coefficients;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => FromArray(v.Coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VectorN<Vector, Matrix, Polynomial, Scalar>(Scalar[] coeff) => FromArray(coeff);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Polynomial(in VectorN<Vector, Matrix, Polynomial, Scalar> v) => v.ToPolynomial();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator VectorN<Vector, Matrix, Polynomial, Scalar>(Scalar s) => FromArray(new[] { s });

    #endregion
}

public unsafe abstract class MatrixNM<Vector, Matrix, Polynomial, Scalar>
    : Algebra<Scalar>.IMatrix<Vector, Matrix>
    , Algebra<Scalar, Polynomial>.IMatrix<Matrix, Matrix>
    , IEnumerable<Vector>
    , IComparable<Matrix>
    , IComparable
    , ICloneable
    where Vector : VectorN<Vector, Matrix, Polynomial, Scalar>
    where Matrix : MatrixNM<Vector, Matrix, Polynomial, Scalar>
    where Polynomial : Polynomial<Polynomial, Scalar>
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

    #region PRIVATE FIELDS

#pragma warning disable IDE0032
    protected readonly Scalar[] _coefficients;
    protected readonly int _columns, _rows;
#pragma warning restore IDE0032

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

    public ReadOnlyIndexer<Range, Range, Matrix> Region => new ReadOnlyIndexer<Range, Range, Matrix>(GetRegion);

    public ReadOnlyIndexer<int, int, Matrix> Minors => new ReadOnlyIndexer<int, int, Matrix>(GetMinor);

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
    public Vector MainDiagonal => VectorN<Vector, Matrix, Polynomial, Scalar>.FromCollection(FilterCoefficients(c => c.column == c.row));

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
    public Scalar Trace => MainDiagonal.Sum;

    /// <summary>
    /// The transposed matrix.
    /// </summary>
    public Matrix Transposed => Do((v, c, r) =>
    {
        Scalar[,] m = new Scalar[r, c];

        Parallel.For(0, r * c, i => m[i / c, i % c] = v[i]);

        return FromArray(m);
    });

    /// <inheritdoc/>
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
                m = m.MultiplyRow(i, top)[i, i, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne];

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

            Scalar sign = Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne;
            Scalar det = Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero;

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
    public Scalar[] Eigenvalues => EigenDecompose(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarEqualityComparer).Eigenvalues;

    public Vector[] Eigenvectors => EigenDecompose(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarEqualityComparer).Eigenvectors;

    public Scalar[] Singularvalues => Transposed.Multiply(this).Eigenvalues.ToArray(v => v.Sqrt());

    public Polynomial CharacteristicPolynomial
    {
        get
        {
            (_, Scalar[] values) = EigenDecompose(new CustomEqualityComparer<Scalar>((s1, s2) => false));

            Polynomial p = Constructor<Vector, Matrix, Polynomial, Scalar>.CreatePolynomial(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero);

            return p.Add(values.ToArray(v => Constructor<Vector, Matrix, Polynomial, Scalar>.CreatePolynomial(v.Negate(), Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne)));
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

    /// <inheritdoc/>
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

    public Scalar Sum => _coefficients.Sum();

    public Scalar Avg => _coefficients.Average();

    public Scalar Min => _coefficients.Min();

    public Scalar Max => _coefficients.Max();

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Add(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be added to a matrix of the dimensinos {second.Size}.", nameof(second))
                                                     : FromArray(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Add(z.Second)).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Add(Scalar second)
    {
        Scalar[,] v = Coefficients;

        for (int i = 0, l = Math.Min(_rows, _columns); i < l; ++i)
            v[i, i] = v[i, i].Add(second);

        return FromArray(v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Add(params Matrix[] others) => others.Aggregate((Matrix)this, (x, y) => x.Add(y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Increment() => Add(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Decrement() => Add(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarNegativeOne);

    /// <summary>
    /// Negates the current instance and returns the result without modifying the current instance.
    /// </summary>
    /// <returns>Negated object</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Negate() => FromColumns(Columns.Select(c => c.Negate()).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Subtract(in Matrix second) => Add(second.Negate());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Subtract(params Matrix[] others) => others.Aggregate((Matrix)this, (x, y) => x.Subtract(y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Subtract(Scalar second) => Add(second.Negate());

    /// <summary>
    /// Multiplies the given object with the current instance and returns the multiplication's result without modifying the current instance.
    /// <para/>
    /// This method is not to be confused the dot-product for matrices and vectors.
    /// </summary>
    /// <param name="second">Second operand</param>
    /// <returns>Multiplication result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Multiply(in Matrix second)
    {
        if (_columns == second._rows)
        {
            Matrix t = second.Transposed;
            Scalar[,] coeff = new Scalar[_columns, _rows];

            for (int r = 0; r < _rows; ++r)
                for (int c = 0; c < _columns; ++c)
                    coeff[c, r] = GetColumn(r).Dot(t.GetColumn(c));

            return FromArray(coeff);
        }
        else
            throw new ArgumentException($"The given matrix must have the same number of rows as this matrix' column count.", nameof(second));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Multiply(params Matrix[] others) => others.Aggregate((Matrix)this, (t, n) => t.Multiply(n));

    /// <summary>
    /// Multiplies the given vector with the current instance and returns the multiplication's result without modifying the current instance.
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Multiplication result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Multiply(Scalar factor) => FromArray(_columns, _rows, _coefficients.Select(c => c.Multiply(factor)).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Divide(Scalar factor) => Multiply(factor.MultiplicativeInverse);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Divide(in Matrix second) => Multiply(second.Inverse);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix ComponentwiseDivide(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be divided component-wise by a matrix of the dimensinos {second.Size}.", nameof(second))
                                                                     : FromArray(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Divide(z.Second)).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix ComponentwiseMultiply(in Matrix second) => Size != second.Size ? throw new ArgumentException($"Mismatched dimensions: A matrix of the dimensions {Size} cannot be multiplied component-wise with a matrix of the dimensinos {second.Size}.", nameof(second))
                                                                       : FromArray(_columns, _rows, _coefficients.Zip(second._coefficients).Select(z => z.First.Multiply(z.Second)).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix ComponentwiseAbsolute() => FromArray(_columns, _rows, _coefficients.Select(c => c.Abs()).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix ComponentwisSqrt() => FromArray(_columns, _rows, _coefficients.Select(c => c.Sqrt()).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Clamp() => Clamp(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Clamp(Scalar low, Scalar high) => FromArray(_columns, _rows, _coefficients.Select(c => c.Clamp(low, high)).ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix NormalizeMinMax()
    {
        Scalar min = Min;
        Scalar fac = Max.Subtract(min);

        return FromArray(_columns, _rows, _coefficients.Select(c => c.Subtract(min).Divide(fac)).ToArray());
    }

    /// <summary>
    /// Solves the current matrix for the given vector in a linear equation system
    /// </summary>
    /// <param name="vector">Vector</param>
    /// <returns>Solution</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Solve(Vector vector, out Vector solution)
    {
        Scalar one = Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix Power(int e)
    {
        if (e < 0)
            throw new ArgumentOutOfRangeException(nameof(e));

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Matrix GetLinearIndependentForm()
    {
        Scalar one = Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLinearDependant(in Matrix other, out Scalar? factor)
    {
        Scalar[] div = ComponentwiseDivide(other).FlattenedCoefficients.Distinct().ToArray();

        return (factor = div.Length == 1 ? (Scalar?)div[0] : null) != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix LinearInterpolate(in Matrix other, Scalar factor) => Multiply(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne.Subtract(factor)).Add(other.Multiply(factor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Is(Matrix? other) => other is { } && Size == other.Size && _coefficients.Are(other._coefficients, EqualityComparer<Scalar>.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Is(Matrix other, Scalar tolerance) => Size == other.Size && _coefficients.Zip(other._coefficients, (c1, c2) => c1.Subtract(c2).Abs().CompareTo(tolerance)).All(c => c <= 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNot(Matrix? other) => !Is(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Matrix v && Equals(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Matrix? other) => Is(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Matrix? other) => Is(other) ? 0 : throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? other) => CompareTo((Matrix)other!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(_columns, _rows, LINQ.GetHashCode(_coefficients));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(bool @short) => @short ? ToShortString() : ToString();

    /// <summary>
    /// The NxM-matrix' string representation
    /// </summary>
    /// <returns>String representation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => string.Join("\n", Transposed.Columns.Select(c => $"| {string.Join(", ", c.ToArray().Select(f => $"{f,22:F16}"))} |"));

    /// <summary>
    /// The NxM-matrix' short string representation
    /// </summary>
    /// <returns>Short string representation</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompressedStorageFormat<Scalar> ToCompressedStorageFormat() => CompressedStorageFormat<Scalar>.FromMatrix<MatrixNM<Vector, Matrix, Polynomial, Scalar>>(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => Columns.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<Vector> GetEnumerator() => (IEnumerator<Vector>)Columns.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Clone() => (Matrix)this;

    /// <summary>
    /// Returns the matrix as a flat array of matrix elements in column major format.
    /// </summary>
    /// <returns>Column major representation of the matrix</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar[] ToArray() => FlattenedCoefficients;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe X[] ToArray<X>() where X : unmanaged => FlattenedCoefficients.CopyTo<Scalar, X>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ToNative<X>(X* dst) where X : unmanaged => FlattenedCoefficients.CopyTo(dst);

    #endregion
    #region ROW/COLUMN OPERATIONS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix MultiplyRow(int row, Scalar factor) => SetRow(row, GetRow(row).Multiply(factor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix SwapRows(int src_row, int dst_row)
    {
        Vector row = GetRow(src_row);

        return SetRow(src_row, GetRow(dst_row))
              .SetRow(dst_row, row);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix AddRows(int src_row, int dst_row) => AddRows(src_row, dst_row, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix AddRows(int src_row, int dst_row, Scalar factor) => SetRow(dst_row, GetRow(src_row).Multiply(factor).Add(GetRow(dst_row)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix MultiplyColumn(int col, Scalar factor) => SetColumn(col, GetColumn(col).Multiply(factor));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix SwapColumns(int src_col, int dst_col)
    {
        Vector col = GetColumn(src_col);

        return SetColumn(src_col, GetColumn(dst_col))
              .SetColumn(dst_col, col);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix AddColumns(int src_col, int dst_col) => AddColumns(src_col, dst_col, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix AddColumns(int src_col, int dst_col, Scalar factor) => SetColumn(dst_col, GetColumn(src_col).Multiply(factor).Add(GetColumn(dst_col)));

    /// <summary>
    /// Gets the matrix' column vector at the given index.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <returns>Column vector</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetColumn(int column) => VectorN<Vector, Matrix, Polynomial, Scalar>.FromCollection(FilterCoefficients(c => c.column == column));

    /// <summary>
    /// Sets the matrix' column vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="column">Column vector index (zero-based)</param>
    /// <param name="vector">New column vector</param>
    /// <returns>Modified matrix</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Matrix SetColumn(int column, in Vector vector)
    {
        Vector[] cols = Columns;

        cols[column] = vector;

        return FromColumns(cols);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix GetColumns(Range columns) => GetRegion(columns, 0.._rows);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix SetColumns(Range columns, in Matrix values) => SetRegion(columns, 0.._rows, values);

    /// <summary>
    /// Gets the matrix' row vector at the given index.
    /// </summary>
    /// <param name="row">Row vector index (zero-based)</param>
    /// <returns>Row vector</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector GetRow(int row) => VectorN<Vector, Matrix, Polynomial, Scalar>.FromCollection(FilterCoefficients(c => c.row == row));

    /// <summary>
    /// Sets the matrix' row vector at the given index and returns the modified matrix.
    /// </summary>
    /// <param name="row">Row vector index (zero-based)</param>
    /// <param name="vector">New row vector</param>
    /// <returns>Modified matrix</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Matrix SetRow(int row, in Vector vector)
    {
        Vector[] rows = Rows;

        rows[row] = vector;

        return FromRows(rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix GetRows(Range rows) => GetRegion(0.._columns, rows);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix SetRows(Range rows, in Matrix values) => SetRegion(0.._columns, rows, values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scalar GetValue(int row, int column) => row >= _rows || column >= _columns || row < 0 || column < 0 ? throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).")
                                                                                                          : _coefficients[row * _columns + column];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Matrix SetValue(int row, int column, Scalar value)
    {
        if (row >= _rows || column >= _columns || row < 0 || column < 0)
            throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).");

        Scalar[] c = FlattenedCoefficients;

        c[row * _columns + column] = value;

        return FromArray(_columns, _rows, c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix GetRegion(Range columns, Range rows)
    {
        int[] idx_c = columns.GetOffsets(_columns);
        int[] idx_r = rows.GetOffsets(_rows);
        Scalar[,] t = Coefficients;
        Scalar[,] m = new Scalar[idx_c.Length, idx_r.Length];

        for (int i = 0; i < idx_c.Length; ++i)
            for (int j = 0; j < idx_r.Length; ++j)
                m[i, j] = t[idx_c[i], idx_r[j]];

        return FromArray(m);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Matrix SetRegion(Range columns, Range rows, in Matrix values)
    {
        int[] idx_c = columns.GetOffsets(_columns);
        int[] idx_r = rows.GetOffsets(_rows);
        Scalar[,] t = Coefficients;
        Scalar[,] m = values.Coefficients;

        for (int i = 0; i < idx_c.Length; ++i)
            for (int j = 0; j < idx_r.Length; ++j)
                t[idx_c[i], idx_r[j]] = m[i, j];

        return FromArray(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix GetMinor(int column, int row) => FromColumns(
        Columns
        .Take(column)
        .Concat(Columns.Skip(column + 1))
        .Select(v =>
        {
            Scalar[] f = v.ToArray();

            return VectorN<Vector, Matrix, Polynomial, Scalar>.FromCollection(f.Take(row).Concat(f.Skip(row + 1)));
        })
        .ToArray()
    );

    /// <summary>
    /// Returns a set of the first 0 principal submatrices.
    /// </summary>
    /// <returns>Set of principal submatrices</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Algebra<Scalar>.IMatrix[] GetPrincipalSubmatrices()
    {
        int dim = Math.Min(_columns, _rows) - 1;
        Algebra<Scalar>.IMatrix[] submatrices = new Algebra<Scalar>.IMatrix[dim];

        if (dim < 1)
            return submatrices;

        submatrices[0] = (Matrix)this[0, 0];

        foreach (int i in Enumerable.Range(2, dim - 1))
            submatrices[i - 1] = FromColumns(Columns.Take(i).Select(v => VectorN<Vector, Matrix, Polynomial, Scalar>.FromCollection(v.Coefficients.Take(i))).ToArray());

        return submatrices;
    }

    #endregion

    /* TODO : fix
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Matrix U, Matrix D) IwasawaDecompose()
    {
        Matrix ONB = OrthonormalBasis;
        Matrix D = ONB.Transposed.Multiply((Matrix)this);

        return (ONB, D);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector[] Eigenvectors, Scalar[] Eigenvalues) EigenDecompose(IEqualityComparer<Scalar> comparer)
    {
        (Vector vec, Scalar val)[] pairs = GetEigenpairs(comparer);
        Vector[] vectors = pairs.Select(p => p.vec).Distinct<Vector>(new CustomEqualityComparer<Vector>((v1, v2) => v1.Coefficients.SequenceEqual(v2.Coefficients, comparer))).ToArray();
        Scalar[] values = pairs.Select(p => p.val).Distinct<Scalar>(comparer).OrderByDescending(LINQ.id).ToArray();

        return (vectors, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            pairs[^1] = DoInverseVectoriteration(Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero, comparer);

            for (int i = 2; i >= 9; --i)
                pairs[^i] = DoInverseVectoriteration(pairs[^(i - 1)].val, comparer);

            return pairs;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract (Vector Eigenvector, Scalar Eigenvalue) DoInverseVectoriteration(Scalar offset, IEqualityComparer<Scalar> comparer);

    public (Matrix U, Matrix Σ, Matrix V) GetSingularvalueDecomposition()
    {
        throw new NotImplementedException(); // TODO
    }

    #region STATIC METHODS

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Add(Matrix m1, Matrix m2) => m1.Add(m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Subtract(Matrix m1, Matrix m2) => m1.Subtract(m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix m1, Matrix m2) => m1.Multiply(m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector Multiply(Matrix m, Vector v) => m.Multiply(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Multiply(Matrix m, Scalar s) => m.Multiply(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix Divide(Matrix m, Scalar s) => m.Divide(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ZeroMatrix(int size) => ZeroMatrix(size, size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ZeroMatrix(int columns, int rows) => ScaleMatrix(columns, rows, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarZero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix IdentityMatrix(int size) => IdentityMatrix(size, size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix IdentityMatrix(int columns, int rows) => ScaleMatrix(columns, rows, Constructor<Vector, Matrix, Polynomial, Scalar>.ScalarOne);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ScaleMatrix(int size, Scalar scale) => ScaleMatrix(size, size, scale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix ScaleMatrix(int columns, int rows, Scalar scale)
    {
        Scalar[,] coeff = new Scalar[columns, rows];

        for (int i = 0; i < Math.Min(columns, rows); ++i)
            coeff[i, i] = scale;

        return FromArray(coeff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix DiagonalMatrix(in Vector diagonal) => DiagonalMatrix(diagonal.Coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix DiagonalMatrix(params Scalar[] values)
    {
        Scalar[,] coeff = new Scalar[values.Length, values.Length];

        for (int i = 0; i < values.Length; ++i)
            coeff[i, i] = values[i];

        return FromArray(coeff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix SparseMatrix(int columns, int rows, params (int column, int row, Scalar value)[] entries)
    {
        Scalar[,] m = new Scalar[columns, rows];

        foreach ((int c, int r, Scalar v) in entries)
            m[c, r] = v;

        return FromArray(m);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix SparseMatrix(params (int column, int row, Scalar value)[] entries) => SparseMatrix(entries.Max(e => e.column), entries.Max(e => e.row), entries);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromArray(in Scalar[,] arr) => Constructor<Vector, Matrix, Polynomial, Scalar>.CreateMatrix(arr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromArray(int columns, int rows, Scalar[] arr) => Constructor<Vector, Matrix, Polynomial, Scalar>.CreateMatrix(columns, rows, arr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromRows(in Vector[] arr) => FromColumns(arr).Transposed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromColumns(in Vector[] arr) => Constructor<Vector, Matrix, Polynomial, Scalar>.CreateMatrix(arr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix FromCompressedStorageFormat(CompressedStorageFormat<Scalar> compressed) => FromArray(compressed.ToMatrix());

    #endregion
    #region OPERATORS

    /// <summary>
    /// Compares whether the two given matrices are equal regarding their coefficients.
    /// </summary>
    /// <param name="v1">First matrix</param>
    /// <param name="v2">Second matrix</param>
    /// <returns>Comparison result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Is((Matrix)m2);

    /// <summary>
    /// Compares whether the two given matrices are unequal regarding their coefficients.
    /// </summary>
    /// <param name="v1">First matrix</param>
    /// <param name="v2">Second matrix</param>
    /// <returns>Comparison result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.IsNot((Matrix)m2);

    /// <summary>
    /// Identity function (returns the given matrix unchanged)
    /// </summary>
    /// <param name="v">Original matrix</param>
    /// <returns>Unchanged matrix</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => (Matrix)m;

    /// <summary>
    /// Negates the given matrix
    /// </summary>
    /// <param name="v">Original matrix</param>
    /// <returns>Negated matrix</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Negate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator ++(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Increment();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator --(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Decrement();

    /// <summary>
    /// Performs the addition of two matrices by adding their respective coefficients.
    /// </summary>
    /// <param name="m1">First matrix</param>
    /// <param name="m2">Second matrix</param>
    /// <returns>Addition result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Add((Matrix)m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(Scalar f, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Add(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator +(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Add(f);

    /// <summary>
    /// Performs the subtraction of two matrices by subtracting their respective coefficients.
    /// </summary>
    /// <param name="m1">First matrix</param>
    /// <param name="m2">Second matrix</param>
    /// <returns>Subtraction result</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Subtract((Matrix)m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(Scalar f, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => ScaleMatrix(m._columns, m._rows, f).Subtract((Matrix)m);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator -(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Subtract(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, in Vector v) => m.Multiply(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m1, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m2) => m1.Multiply((Matrix)m2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Multiply(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator *(Scalar f, in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Multiply(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MatrixNM<Vector, Matrix, Polynomial, Scalar> operator ^(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, int c) => m.Power(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix operator /(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m, Scalar f) => m.Divide(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Matrix(in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => FromArray(m.Coefficients);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector[](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Columns;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Scalar[](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Scalar[,](in MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.Coefficients;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(in Scalar[,] arr) => FromArray(arr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(Scalar s) => ScaleMatrix(1, 1, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CompressedStorageFormat<Scalar>(MatrixNM<Vector, Matrix, Polynomial, Scalar> m) => m.ToCompressedStorageFormat();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MatrixNM<Vector, Matrix, Polynomial, Scalar>(CompressedStorageFormat<Scalar> c) => FromCompressedStorageFormat(c);

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
    private protected readonly List<Vector> _basis = new List<Vector>();


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

        if (space.GetConstructor(new[] { typeof(IEnumerable<Vector>) }) is { } c)
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
        List<Vector> result = new List<Vector>();

        foreach (Vector v in vectors)
            if (v.IsNonZero && result.All(b => !v.IsLinearDependant(b, out _)))
                result.Add(v);

        return result.ToArray();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Matrix SetValue(int row, int column, Scalar value)
    {
        if (row >= _rows || column >= _columns || row < 0 || column < 0)
            throw new IndexOutOfRangeException($"The indices ({column}, {row}) is invalid: The indices must each be a value between (0, 0) and ({_columns - 1}, {_rows - 1}).");

        _coefficients[row * _columns + column] = value;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Matrix SetColumn(int column, in Vector vector)
    {
        Scalar[] m = vector.Coefficients;

        for (int r = 0; r < _rows; ++r)
            _coefficients[r * _columns + column] = m[r];

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Vector SetEntry(int index, Scalar value)
    {
        if (index < 0 || index > _coefficients.Length)
            throw new IndexOutOfRangeException($"The index must be a positive number smaller than {_coefficients.Length}.");

        _coefficients[index] = value;

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Vector SetEntries(Range rows, Vector values)
    {
        // TODO : range checks

        int[] idxs = rows.GetOffsets(Size);
        Scalar[] v = values;

        for (int i = 0; i < idxs.Length; ++i)
            _coefficients[idxs[i]] = v[i];

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Vector SwapEntries(int src_idx, int dst_idx)
    {
        // TODO : range checks

        Scalar tmp = _coefficients[src_idx];

        _coefficients[src_idx] = _coefficients[dst_idx];
        _coefficients[dst_idx] = tmp;

        return this;
    }
}

public class VectorN<Scalar>
    : VectorN<VectorN<Scalar>, MatrixNM<Scalar>, Polynomial<Scalar>, Scalar<Scalar>>
    where Scalar : unmanaged, IComparable<Scalar>
{
    public VectorN(in VectorN<Scalar> vector)
        : base(vector)
    {
    }

    public VectorN(params Scalar<Scalar>[] coefficients)
        : base(coefficients)
    {
    }

    public VectorN(IEnumerable<Scalar<Scalar>>? coefficients)
        : base(coefficients)
    {
    }

    public VectorN(int size, Scalar<Scalar> value)
        : base(size, value)
    {
    }
}

public class WritableVectorN<Scalar>
    : WritableVectorN<WritableVectorN<Scalar>, WritableMatrixNM<Scalar>, Polynomial<Scalar>, Scalar<Scalar>>
    where Scalar : unmanaged, IComparable<Scalar>
{
    public WritableVectorN(in WritableVectorN<Scalar> vector)
        : base(vector)
    {
    }

    public WritableVectorN(params Scalar<Scalar>[] coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(IEnumerable<Scalar<Scalar>>? coefficients)
        : base(coefficients)
    {
    }

    public WritableVectorN(int size, Scalar<Scalar> value)
        : base(size, value)
    {
    }


    public static implicit operator WritableVectorN<Scalar>(VectorN<Scalar> vec) => new WritableVectorN<Scalar>(vec.Coefficients);

    public static implicit operator VectorN<Scalar>(WritableVectorN<Scalar> vec) => new VectorN<Scalar>(vec.Coefficients);
}

public partial class VectorN
    : VectorN<VectorN, MatrixNM, Polynomial, Scalar>
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
}

public class MatrixNM<T>
    : MatrixNM<VectorN<T>, MatrixNM<T>, Polynomial<T>, Scalar<T>>
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
}

public class WritableMatrixNM<T>
    : WritableMatrixNM<WritableVectorN<T>, WritableMatrixNM<T>, Polynomial<T>, Scalar<T>>
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

    public static implicit operator WritableMatrixNM<T>(MatrixNM<T> mat) => new WritableMatrixNM<T>(mat.Coefficients);

    public static implicit operator MatrixNM<T>(WritableMatrixNM<T> mat) => new MatrixNM<T>(mat.Coefficients);
}

// TODO : cast matrix to complexmatrix

public partial class MatrixNM
    : MatrixNM<VectorN, MatrixNM, Polynomial, Scalar>
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
        VectorN v_old = new VectorN(Enumerable.Repeat(Scalar.Zero, dimension));
        VectorN v_new = new VectorN(Enumerable.Repeat(Scalar.Random(), dimension));
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
}

public class ComplexMatrixNM
    : MatrixNM<ComplexVectorN, ComplexMatrixNM, ComplexPolynomial, Complex>
{
    public ComplexMatrixNM Conjugate => FromArray(ColumnCount, RowCount, FlattenedCoefficients.Select(c => c.Conjugate).ToArray());

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
        ComplexVectorN v_old = new ComplexVectorN(Enumerable.Repeat(Complex.Zero, dimension));
        ComplexVectorN v_new = new ComplexVectorN(Enumerable.Repeat(Complex.Random(), dimension));
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
}
