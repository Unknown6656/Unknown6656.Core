using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System;

using static System.Math;

namespace Unknown6656.Mathematics.Analysis;


public class FunctionCache<F, I, V>
    : Function<F, I, V>
    , IDisposable
    where F : Function<F, I, V>
    where I : IEquatable<I>
{
    public const uint DEFAULT_CACHE_SIZE = 1024 * 1024 * 256;
    private readonly Dictionary<I, V> _valdic = [];


    public ReadOnlyIndexer<I, bool> IsCached { get; }

    public double UsedCacheRatio => (double)UsedCacheEntries / CacheSize;

    public int UsedCacheEntries => _valdic.Count;

    public int CacheSize { get; }

    public F Function { get; }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    [Obsolete("Use the variable directly instead.", true)]
    public override FunctionCache<F, I, V> Cached => throw new InvalidOperationException("A function cache cannot be further cached.");
#pragma warning restore CS0809

    public override bool IsZero => Function.IsZero;


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

    public override V Evaluate(I x)
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

    public override F Negate() => Function.Negate().Cached;

    public override F Add(in F second) => Function.Add(in second).Cached;

    public override F Add(params F[] others) => others.Aggregate(this, (x, y) => x.Add(in y));

    public override F Subtract(in F second) => Function.Subtract(in second).Cached;

    public override F Subtract(params F[] others) => others.Aggregate(this, (x, y) => x.Subtract(in y));

    public override bool Equals(F other) => Function.Is(other);

    //public override bool Equals(object? other) => other is F o && Equals(o);

    public override int GetHashCode() => Function.GetHashCode();

    public override unsafe string ToString()
    {
        int size_i = typeof(I).IsClass ? sizeof(GCHandle) : Marshal.SizeOf<I>();
        int size_v = typeof(V).IsClass ? sizeof(GCHandle) : Marshal.SizeOf<V>();
        long sz = 24 + UsedCacheEntries * (4L + size_i + size_v);

        return $"{Function} [{sz.ToHumanReadableSize()} / {MathExtensions.ToHumanReadableSize(CacheSize)} ≈ {UsedCacheRatio * 100:F2}% in use]";
    }


    public static implicit operator FunctionCache<F, I, V>(F function) => new(function);

    public static implicit operator F(FunctionCache<F, I, V> cache) => cache.Function;
}
