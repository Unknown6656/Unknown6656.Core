using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Analysis;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Statistics;

// TODO : implement from Unknown6656.Mathematics.Analysis.DensityFunction<T>


/// <summary>
/// Rpresents an abstract, generic data set for regression calculations.
/// </summary>
/// <typeparam name="Scalar">Generic data parameter type</typeparam>
public interface IRegressionDataSet<Set, Scalar>
    //: ISet<Scalar>
    : IEnumerable<Scalar>
    , ICloneable
    where Set : IRegressionDataSet<Set, Scalar>
    where Scalar : IComparable<Scalar>
{
    /// <summary>
    /// Returns the sum of the current data set.
    /// </summary>
    Scalar Sum { get; }

    /// <summary>
    /// Returns the Average of the current data set.
    /// </summary>
    Scalar Average { get; }

    /// <summary>
    /// Returns the variance of the current data set.
    /// </summary>
    Scalar Variance { get; }

    /// <summary>
    /// Returns the median of the current data set.
    /// </summary>
    Scalar Median { get; }

    /// <summary>
    /// Returns the standard deviation of the current data set.
    /// </summary>
    Scalar StandardDeviation { get; }

    /// <summary>
    /// Retruns the variation coefficient of the current data set.
    /// </summary>
    Scalar VariationCoefficient { get; }

    /// <summary>
    /// Returns the current data set in a sorted order.
    /// </summary>
    Set Sorted { get; }

    /// <summary>
    /// The number of elements in the current data set.
    /// </summary>
    int Count { get; }

    void Clear();

    /// <summary>
    /// Adds the given value to the end of the current collection.
    /// </summary>
    /// <param name="value">Value to be added</param>
    /// <returns>Current data set</returns>
    void Add(Scalar value);

    /// <summary>
    /// Adds the given value collection to the end of the current one.
    /// </summary>
    /// <param name="values">Values to be added</param>
    void AddRange(IEnumerable<Scalar> values);

    /// <inheritdoc cref="ICloneable.Clone"/>
    new Set Clone();

    object ICloneable.Clone() => Clone();
}

public unsafe class RegressionDataSet1D
    : IRegressionDataSet<RegressionDataSet1D, Scalar>
{
    #region FIELDS / CONSTANTS

    protected readonly List<Scalar> _values = [];

    #endregion
    #region INDEXERS

    public virtual RegressionDataSet1D this[int start, int count]
    {
        set => this[start..(start + count)] = value;
        get => this[start..(start + count)];
    }

    public virtual RegressionDataSet1D this[Range range]
    {
        set
        {
            int[] idxs = range.GetOffsets(Count);

            if (idxs.Length != value.Count)
                throw new ArgumentOutOfRangeException(nameof(value), $"The given data set must have an length of {idxs.Length} to be inserted in the range '{range}'.");

            for (int i = 0; i < idxs.Length; ++i)
                this[i] = value[i];
        }
        get
        {
            int[] idxs = range.GetOffsets(Count);

            if (idxs.Length == 0)
                return [];
            else if (idxs.Length > Count)
                throw new ArgumentOutOfRangeException(nameof(range), $"The given range '{range}' must not be longer than the total element count of the current regression data set ({Count}).");
            else if (idxs[0] < 0)
                throw new ArgumentOutOfRangeException(nameof(range), $"The given range '{range}' must begin with a positive index.");
            else if (idxs[^1] >= Count)
                throw new ArgumentOutOfRangeException(nameof(range), $"The given range '{range}' must end with an index smaller than the total element count of the current regression data set ({Count}).");

            return new RegressionDataSet1D(idxs.Select(i => _values[i]));
        }
    }

    public virtual Scalar this[Index index]
    {
        set
        {
            int idx = index.GetOffset(Count);

            if (idx < 0 || idx > Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"The index must be a positive integer between (inclusive) 0 and {Count}.");
            else if (idx == Count)
                Add(value);
            else
            {
                Sorted.Remove(_values[index]);
                _values[index] = value;
                Sorted.Add(value);
            }
        }
        get
        {
            int idx = index.GetOffset(Count);

            return idx >= 0 && idx < Count ? _values[index] : throw new ArgumentOutOfRangeException(nameof(index), $"The index must be a positive integer between (inclusive) 0 and {Count - 1}.");
        }
    }

    #endregion
    #region INSTANCE PROPERTIES

    public virtual SortedRegressionDataSet1D Sorted { get; }

    RegressionDataSet1D IRegressionDataSet<RegressionDataSet1D, Scalar>.Sorted => Sorted;

    public virtual ProbabilityQuery1D ProbabilityFor => Sorted.ProbabilityFor;

    public int Count => _values.Count;

    public virtual Scalar Sum => Sorted.Sum;

    public Scalar Average => Sum / Count;

    public virtual Scalar Variance
    {
        get
        {
            if (Count < 2)
                throw new InvalidOperationException("The data set needs at least two elements to calculate the variance or standard deviation.");

            Scalar avg = Average;

            return _values.Sum(i => (i - avg).Power(2)) / (Count - 1);
        }
    }

    public Scalar StandardDeviation => Variance.Sqrt();

    public Scalar VariationCoefficient => StandardDeviation / Average;

    public virtual Scalar Minimum => Sorted.Minimum;

    public virtual Scalar Maximum => Sorted.Maximum;

    public Scalar Median => GetQuantile(.5);

    public Scalar LowerQuartile => GetQuantile(.25);

    public Scalar UpperQuartile => GetQuantile(.75);

    public Scalar QuartileDistance => UpperQuartile - LowerQuartile;

    #endregion
    #region .CTORs

    /// <summary>
    /// Creates a new instance from the given data collection
    /// </summary>
    /// <param name="data">Data collection</param>
    public RegressionDataSet1D(params Scalar[] data)
        : this(data as IEnumerable<Scalar>)
    {
    }

    /// <summary>
    /// Creates a new instance from the given data collection
    /// </summary>
    /// <param name="data">Data collection</param>
    public RegressionDataSet1D(IEnumerable<Scalar> data)
    {
        Sorted = new SortedRegressionDataSet1D(Array.Empty<Scalar>());
        AddRange(data);
    }

    #endregion
    #region STATS METHODS

    public virtual Scalar GetQuantile(Scalar α) => Sorted.GetQuantile(α);

    // TODO : distributions
    // TODO : buckets

    #endregion
    #region SET METHODS

    public virtual void Add(Scalar value)
    {
        _values.Add(value);
        Sorted.Add(value);
    }

    public virtual void AddRange(IEnumerable<Scalar> values)
    {
        _values.AddRange(values);
        Sorted.AddRange(values);
    }

    public virtual void InsertAt(Index index, Scalar value)
    {
        _values.Insert(index.GetOffset(_values.Count), value);
        Sorted.Add(value);
    }

    public virtual void InsertAt(Index index, IEnumerable<Scalar> values)
    {
        _values.InsertRange(index.GetOffset(_values.Count), values);
        Sorted.AddRange(values);
    }

    public virtual void Clear()
    {
        _values.Clear();
        Sorted.Clear();
    }

    public RegressionDataSet1D Clone() => new(_values);

    public IEnumerator<Scalar> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public virtual RegressionDataSet2D To2D() => new(_values.Select((y, i) => ((Scalar)i, y)));

    #endregion
    #region OPERATORS

    public static implicit operator Scalar[](RegressionDataSet1D data) => data.ToArray();

    public static implicit operator List<Scalar>(RegressionDataSet1D data) => data.ToList();

    public static implicit operator RegressionDataSet1D(List<Scalar> data) => new(data);

    public static implicit operator RegressionDataSet1D(Scalar[] data) => new(data);

    #endregion
}

public sealed class SortedRegressionDataSet1D
    : RegressionDataSet1D
{
    private Scalar _sum;


    public override Scalar Sum => _sum;

    public override SortedRegressionDataSet1D Sorted => this;

    public override ProbabilityQuery1D ProbabilityFor { get; }


    internal SortedRegressionDataSet1D(IEnumerable<Scalar> data) : base(data)
    {
        _sum = Scalar.Zero;
        ProbabilityFor = new ProbabilityQuery1D(this);
    }

    public override Scalar GetQuantile(Scalar α)
    {
        Scalar k = α * Count;
        Scalar res = _values[(int)k + 1];

        if (res.IsZero)
            return (res + _values[(int)k]) / 2;
        else
            return res;
    }

    public override void Add(Scalar value)
    {
        _sum += value;

        if (_values.Count == 0 || value < _values[0])
            _values.Insert(0, value);
        else if (value > _values[^1])
            _values.Add(value);
        else
            for (int i = 0, c = _values.Count; i < c - 1; ++c)
            {
                Scalar curr = _values[i];
                Scalar next = _values[i + 1];

                if (curr <= value && value < next)
                {
                    _values.Insert(i, value);

                    return;
                }
            }

        // TODO : unit test this!!!
    }

    public override void AddRange(IEnumerable<Scalar> values)
    {
        foreach (Scalar v in values)
            Add(v);
    }

    public override void Clear()
    {
        _values.Clear();
        _sum = Scalar.Zero;
    }

    internal void Remove(Scalar value)
    {
        _values.Remove(value);
        _sum -= value;
    }

    internal int GetNearestIndexOf(Scalar value, bool strict_smaller)
    {
        /// binary search
        int get_indexof()
        {
            int lo = 0;
            int hi = Count - 1;

            while (lo <= hi)
            {
                int i = (hi + lo) / 2;
                int c = value.CompareTo(_values[i]);

                if (c == 0)
                    return i;
                else if (c > 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return lo;
        }

        int index = get_indexof();

        if (strict_smaller)
            while (index > 0 && _values[index] > value)
                --index;

        return index;
    }

    #region UNSUPPORTED
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

    private const string UNSUPPORTED_MSG = "Indexing operations are not supported on instances of the type " + nameof(SortedRegressionDataSet1D) + ".";

    [Obsolete(UNSUPPORTED_MSG, true)]
    public override Scalar this[Index index]
    {
        get => throw new NotSupportedException(UNSUPPORTED_MSG);
        set => throw new NotSupportedException(UNSUPPORTED_MSG);
    }

    [Obsolete(UNSUPPORTED_MSG, true)]
    public override RegressionDataSet1D this[Range range] => throw new NotSupportedException(UNSUPPORTED_MSG);

    [Obsolete(UNSUPPORTED_MSG, true)]
    public override RegressionDataSet1D this[int start, int count] => throw new NotSupportedException(UNSUPPORTED_MSG);

    [Obsolete(UNSUPPORTED_MSG, true)]
    public override void InsertAt(Index index, IEnumerable<Scalar> values) => throw new NotSupportedException(UNSUPPORTED_MSG);

    [Obsolete(UNSUPPORTED_MSG, true)]
    public override void InsertAt(Index index, Scalar value) => throw new NotSupportedException(UNSUPPORTED_MSG);

#pragma warning restore CS0809
    #endregion
}

public class ProbabilityQuery1D
{
    private readonly SortedRegressionDataSet1D _sorted;


    public ProbabilityQuery1D(SortedRegressionDataSet1D sorted) => _sorted = sorted;

    public Scalar ValueSmallerThan(Scalar v) => _sorted.GetNearestIndexOf(v, true) / (Scalar)_sorted.Count;

    public Scalar ValueSmallerOrEqualTo(Scalar v) => _sorted.GetNearestIndexOf(v, false) / (Scalar)_sorted.Count;

    public Scalar ValueGreaterThan(Scalar v) => 1 - ValueSmallerOrEqualTo(v);

    public Scalar ValueGreaterOrEqualTo(Scalar v) => 1 - ValueSmallerThan(v);

    public Scalar ValueBetweenInclusive(Scalar min, Scalar max) => 1 - ValueNotBetweenInclusive(min, max);

    public Scalar ValueBetweenExclusive(Scalar min, Scalar max) => 1 - ValueNotBetweenExclusive(min, max);

    public Scalar ValueNotBetweenInclusive(Scalar min, Scalar max) => ValueSmallerThan(min) + ValueGreaterThan(max);

    public Scalar ValueNotBetweenExclusive(Scalar min, Scalar max) => ValueSmallerOrEqualTo(min) + ValueGreaterOrEqualTo(max);

    public Scalar ValueEqualTo(Scalar v) => ValueEqualTo(v, Scalar.ComputationalEpsilon);

    public Scalar ValueNotEqualTo(Scalar v) => ValueNotEqualTo(v, Scalar.ComputationalEpsilon);

    public Scalar ValueEqualTo(Scalar v, Scalar tolerance) => _sorted.Count(s => s.Is(v, tolerance)) / (Scalar)_sorted.Count;

    public Scalar ValueNotEqualTo(Scalar v, Scalar tolerance) => 1 - ValueEqualTo(v, tolerance);
}

public class RegressionDataSet2D
    : IRegressionDataSet<RegressionDataSet2D, (Scalar X, Scalar Y)>
{
    private readonly RegressionDataSet1D _x, _y;


    public RegressionDataSet1D XDimension => _x.Clone();

    public RegressionDataSet1D YDimension => _y.Clone();

    public (Scalar X, Scalar Y) Sum => (_x.Sum, _y.Sum);

    public (Scalar X, Scalar Y) Average => (_x.Average, _y.Average);

    public (Scalar X, Scalar Y) Variance => (_x.Variance, _y.Variance);

    public (Scalar X, Scalar Y) Median => (_x.Median, _y.Median);

    public (Scalar X, Scalar Y) StandardDeviation => (_x.StandardDeviation, _y.StandardDeviation);

    public (Scalar X, Scalar Y) VariationCoefficient => (_x.VariationCoefficient, _y.VariationCoefficient);

    /// <summary>
    /// Returns the pearson correlation coefficient of the current data set.
    /// </summary>
    public Scalar PearsonCorrelationCoefficient => (_x.Sum + _y.Sum - (Count * _x.Average * _y.Average)) / ((Count - 1) * _x.StandardDeviation * _y.StandardDeviation);

    /// <summary>
    /// Returns the coefficients A and B to create a linear regression curve 'f(x) = A + B*x'.
    /// </summary>
    public (Scalar A, Scalar B) LinearRegression
    {
        get
        {
            double b = PearsonCorrelationCoefficient * _y.StandardDeviation / _x.StandardDeviation;
            double a = _y.Average - (b * _x.Average);

            return (a, b);
        }
    }

    /// <summary>
    /// Returns the coefficients A, B, and C to create a quadratic regression curve 'f(x) = A + B*x + C*x²'.
    /// </summary>
    public (Scalar A, Scalar B, Scalar C) QuadraticRegression
    {
        get
        {
            ReadOnlyCollection<Scalar> coeff = GetRegressionPolynomial(2).Coefficients;

            return (coeff[0], coeff[1], coeff[2]);
        }
    }

    // TODO : covariance

    RegressionDataSet2D IRegressionDataSet<RegressionDataSet2D, (Scalar X, Scalar Y)>.Sorted => throw new NotImplementedException(); // TODO

    public RegressionDataSet2D SortedByXDimension => new(this.OrderBy(t => t.X));

    public RegressionDataSet2D SortedByYDimension => new(this.OrderBy(t => t.Y));

    public int Count => Math.Min(_x.Count, _y.Count);


    /// <summary>
    /// Creates a new instance from the given 2D data collection.
    /// </summary>
    /// <param name="data">Data collection</param>
    public RegressionDataSet2D(params (Scalar X, Scalar Y)[] data)
        : this(data as IEnumerable<(Scalar, Scalar)>)
    {
    }

    /// <summary>
    /// Creates a new instance from the given 2D data collection.
    /// </summary>
    /// <param name="data">Data collection</param>
    public RegressionDataSet2D(IEnumerable<(Scalar X, Scalar Y)> data)
    {
        _x = [];
        _y = [];

        AddRange(data);
    }

    public Polynomial GetRegressionPolynomial(int degree)
    {
        int rows = _x.Count;
        int cols = degree + 1;

        Scalar[,] mat = new Scalar[cols, rows];

        for (int i = 0; i < rows; ++i)
            for (int j = 0; j < cols; ++j)
                mat[j, i] = _x[i] ^ j;

        MatrixNM V = new(mat);
        (MatrixNM Q, MatrixNM R) = V.QRDecompose();

        R = R[..cols, ..cols];

        MatrixNM RI = R.Inverse;
        VectorN y = (VectorN)_y.ToArray();

        Q = V * RI;
        y = RI * (Q.Transposed * y);

        return new Polynomial([.. y]);
    }

    public void Add(Scalar x, Scalar y)
    {
        _x.Add(x);
        _y.Add(y);
    }

    public void Add((Scalar X, Scalar Y) value) => Add(value.X, value.Y);

    public void AddRange(IEnumerable<(Scalar X, Scalar Y)> values)
    {
        foreach ((Scalar x, Scalar y) in values)
            Add(x, y);
    }

    public void Clear()
    {
        _x.Clear();
        _y.Clear();
    }

    public RegressionDataSet2D Clone() => new(this);

    public RegressionDataSet2D SwapDimensions() => new(ToEnumerable().Swap());

    public IEnumerable<(Scalar X, Scalar Y)> ToEnumerable() => _x.Zip(_y);

    public IEnumerator<(Scalar X, Scalar Y)> GetEnumerator() => ToEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
