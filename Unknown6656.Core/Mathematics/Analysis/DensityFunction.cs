using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Common;


namespace Unknown6656.Mathematics.Analysis
{

    // TODO : move this to Unknown6656.Mathematics.Statistics.RegressionDataSet1D


    /// <summary>
    /// Represents a density function of set of items of the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Generic comparable item type.</typeparam>
    public class DensityFunction<T>
        : IEnumerable<(T Key, T[] Items, Scalar Probability)>
        where T : IComparable<T>
    {
        private protected readonly (T key, List<T> items, Scalar prob)[] _items;

        #region PROPERTIES

        /// <summary>
        /// Returns the total quantity of items in the underlying set.
        /// </summary>
        public int TotalQuantity { get; }

        /// <summary>
        /// Returns the count of item bins -- meaning the number of subsets containing items considered to be equal.
        /// </summary>
        public int BinCount => _items.Length;

        /// <summary>
        /// Returns the set of all items represented by this histogram.
        /// </summary>
        public T[] AllItems => _items.SelectMany(i => i.items).ToArray();

        /// <summary>
        /// Returns the smallest value contained in the underlying dataset.
        /// </summary>
        public T SmallestValue => _items[0].key;

        /// <summary>
        /// Returns the largest value contained in the underlying dataset.
        /// </summary>
        public T GreatestValue => _items[^1].key;

        /// <summary>
        /// Returns the bin associated with the given item. This is the subset of all items considered to be equal with the given one.
        /// </summary>
        public ReadOnlyIndexer<T, T[]> Bin { get; }

        /// <summary>
        /// Returns the quantity of the given item in the underlying set. This is equal to the size of the bin associated with the given item.
        /// </summary>
        public ReadOnlyIndexer<T, int> Quantity { get; }

        /// <summary>
        /// Returns the relative probabilty of the given item in the underlying set. The probabiltiy is constrained to the numbers [0..1].
        /// </summary>
        public ReadOnlyIndexer<T, Scalar> Probability { get; }

        #endregion
        #region CONSTRUCTORS

        // TODO : add bin size etc.
        // https://en.wikipedia.org/wiki/Histogram

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DensityFunction(IEnumerable<T> collection)
            : this(collection is T[] arr ? arr : collection.ToArray())
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DensityFunction(params T[] collection)
        {
            Scalar len = collection.Length;

            if (len.IsZero)
                throw new InvalidOperationException("The given collection must not be empty.");

            _items = (from e in collection
                      group e by e into g
                      orderby g.Key
                      let arr = g.ToList()
                      select (g.Key, arr, arr.Count / len)).ToArray();
            TotalQuantity = (int)len;
            Bin = new ReadOnlyIndexer<T, T[]>(t => Get(t).items.ToArray());
            Quantity = new ReadOnlyIndexer<T, int>(t => Get(t).items.Count);
            Probability = new ReadOnlyIndexer<T, Scalar>(t => Get(t).probability);
        }

        #endregion
        #region PRIVATE METHODS

        private int? GetBinIndex(T key)
        {
            for (int i = 0; i < _items.Length; ++i)
                if (_items[i].Equals(key))
                    return i;

            return null;
        }

        private (List<T> items, Scalar probability) Get(T key)
        {
            for (int i = 0; i < _items.Length; ++i)
            {
                (T k, List<T> items, Scalar prob) = _items[i];

                if (k.Equals(key))
                    return (items, prob);
            }

            return (new List<T>(), 0);
        }

        #endregion
        #region INSTANCE METHODS

        /// <summary>
        /// Returns a collection of all items equal or smaller than the given value.
        /// </summary>
        /// <param name="value">Value to be compared with.</param>
        /// <returns>Collection of items equal to or smaller than the given value.</returns>
        public T[] ItemsSmallerThan(T value) => GetBinIndex(value) is int idx ? _items.Take(idx + 1).SelectMany(t => t.items).ToArray() : Array.Empty<T>();

        /// <summary>
        /// Returns the cumulative probability of all items equal or smaller than the given value.
        /// </summary>
        /// <param name="value">Value to be compared with.</param>
        /// <returns>Cumulative probability of items equal to or smaller than the given value.</returns>
        public Scalar ProbabilityOfSmallerThan(T value) => GetBinIndex(value) is int idx ? (Scalar)_items.Take(idx + 1).Sum(t => (double)t.prob) : Scalar.Zero;

        /// <summary>
        /// Returns a collection of all items equal or greater than the given value.
        /// </summary>
        /// <param name="value">Value to be compared with.</param>
        /// <returns>Collection of items equal to or greater than the given value.</returns>
        public T[] ItemsGreaterThan(T value) => GetBinIndex(value) is int idx ? _items.Skip(idx).SelectMany(t => t.items).ToArray() : Array.Empty<T>();

        /// <summary>
        /// Returns the cumulative probability of all items equal or greater than the given value.
        /// </summary>
        /// <param name="value">Value to be compared with.</param>
        /// <returns>Cumulative probability of items equal to or greater than the given value.</returns>
        public Scalar ProbabilityOfGreaterThan(T value) => GetBinIndex(value) is int idx ? (Scalar)_items.Skip(idx).Sum(t => (double)t.prob) : Scalar.Zero;




        public IEnumerator<(T Key, T[] Items, Scalar Probability)> GetEnumerator() => _items.Select(t => (t.key, t.items.ToArray(), t.prob)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
        #region STATIC METHODS
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DensityFunction<T> FromCollection(IEnumerable<T> collection) => new DensityFunction<T>(collection);

        #endregion
    }
}
