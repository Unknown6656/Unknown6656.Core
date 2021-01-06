#nullable enable

using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Common;

using static System.Math;

namespace Unknown6656.Mathematics.Analysis
{
    public class FunctionCache<F, I, V>
        : IRelation<F, I, V>
        , IDisposable
        where F : IRelation<F, I, V>
        where I : IEquatable<I>
    {
        public const uint DEFAULT_CACHE_SIZE = 1024 * 1024 * 256;
        private readonly Dictionary<I, V> _valdic = new Dictionary<I, V>();


        public ReadOnlyIndexer<I, bool> IsCached { get; }

        public double UsedCacheRatio => (double)UsedCacheEntries / CacheSize;

        public int UsedCacheEntries => _valdic.Count;

        public V this[I x] => Evaluate(x);

        public int CacheSize { get; }

        public F Function { get; }

        [Obsolete("Use the variable directly instead.", true)]
        public FunctionCache<F, I, V> Cached => throw new InvalidOperationException("A function cache cannot be further cached.");

        public F AdditiveInverse => Negate();

        public bool IsZero => Function.IsZero;

        public bool IsNonZero => !IsZero;


        ~FunctionCache() => Dispose();

        static FunctionCache()
        {
            Type F = typeof(F);

            if (F.IsConstructedGenericType)
                if (typeof(FunctionCache<,,>).IsAssignableFrom(F.GetGenericTypeDefinition()))
                    throw new ArgumentException("The given function cannot be a function cache in itself.");
        }

        public FunctionCache(F function)
            : this(function, DEFAULT_CACHE_SIZE)
        {
        }

        public FunctionCache(F function, uint size)
        {
            Function = function;
            CacheSize = (int)size;
            IsCached = new ReadOnlyIndexer<I, bool>(_valdic.ContainsKey);
        }

        public void Dispose()
        {
            ClearCache();

            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void ClearCache()
        {
            lock (_valdic)
                _valdic.Clear();
        }

        public virtual V Evaluate(I x)
        {
            if (_valdic.TryGetValue(x, out V v))
                return v;
            else
            {
                V res = Function.Evaluate(x);

                lock (_valdic)
                {
                    if (_valdic.Count > CacheSize)
                        foreach (I k in _valdic.Keys.Take(Min(100, _valdic.Count)).ToArray())
                            _valdic.Remove(k);

                    _valdic.Add(x, res);
                }

                return res;
            }
        }

        public F Negate() => Function.Negate().Cached;

        public F Add(in F second) => Function.Add(in second).Cached;

        public F Add(params F[] others) => others.Aggregate(this, (x, y) => x.Add(in y));

        public F Subtract(in F second) => Function.Subtract(in second).Cached;

        public F Subtract(params F[] others) => others.Aggregate(this, (x, y) => x.Subtract(in y));

        public bool Is(F o) => Function.Is(o);

        public bool IsNot(F o) => !Is(o);

        public bool Equals(F other) => Is(other);

        public override bool Equals(object? other) => other is F o && Equals(o);

        public override int GetHashCode() => Function.GetHashCode();

        public override string ToString()
        {
            string pref = "";

            if (!typeof(I).IsClass && !typeof(V).IsClass)
            {
                long sz = 24 + UsedCacheEntries * (4L + Marshal.SizeOf<I>() + Marshal.SizeOf<V>());

                pref = $"{sz.ToHumanReadableSize()} / {MathExtensions.ToHumanReadableSize(CacheSize)} ≈ ";
            }

            return $"{Function} [{pref}{UsedCacheRatio * 100:F2}% in use]";
        }


        public static implicit operator FunctionCache<F, I, V>(F function) => new FunctionCache<F, I, V>(function);

        public static implicit operator F(FunctionCache<F, I, V> cache) => cache.Function;
    }
}
