using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.Mathematics.Graphs;
using Unknown6656.IO;

using static System.Math;

namespace Unknown6656.Common;

public static class LINQExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DensityFunction<T> GenerateDensityFunction<T>(this IEnumerable<T> collection) where T : IComparable<T> => new(collection);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte[] BinaryCast<T>(this T value) where T : unmanaged => BinaryCast(&value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte[] BinaryCast<T>(T* pointer) where T : unmanaged => DataStream.FromPointer(pointer).ToBytes();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T BinaryCast<T>(this byte[] source) where T : unmanaged => DataStream.FromBytes(source).ToUnmanaged<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe U BinaryCast<T, U>(this T value) where T : unmanaged where U : unmanaged => *(U*)&value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe U[] CopyTo<T, U>(this T[] source) where T : unmanaged where U : unmanaged => source.CopyTo<T, U>(sizeof(T) * source.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe U[] CopyTo<T, U>(this T[] source, int byte_count)
        where T : unmanaged
        where U : unmanaged
    {
        U[] dest = new U[(int)Ceiling(byte_count / (float)sizeof(U))];

        fixed (U* ptrd = dest)
            source.CopyTo(ptrd, byte_count);

        return dest;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyTo<T, U>(this T[] source, U* target) where T : unmanaged where U : unmanaged => source.CopyTo(target, sizeof(T) * source.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyTo<T, U>(this T[] source, U* target, int byte_count)
        where T : unmanaged
        where U : unmanaged
    {
        fixed (T* ptrs = source)
        {
            byte* bptrd = (byte*)target;
            byte* bptrs = (byte*)ptrs;

            for (int i = 0; i < byte_count; ++i)
                bptrd[i] = bptrs[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AggregateNonEmpty<T>(this IEnumerable<T> coll, Func<T, T, T> accumulator/*, U init = default*/)
    {
        List<T>? list = coll?.ToList();

        if (list is null)
            throw new ArgumentNullException(nameof(coll));
        else if (list.Count < 2)
            throw new ArgumentException("The given collection must have more than one element.", nameof(coll));

        T result = accumulator(list[0], list[1]);

        foreach (T t in list.Skip(2))
            result = accumulator(result, t);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AggregateNonEmpty<T>(this IEnumerable<T> coll, Func<T, T, T> accumulator, T init) => coll?.ToList() switch
    {
        null => throw new ArgumentNullException(nameof(coll)),
        { Count: 0 } => init,
        List<T> list => Do(() =>
        {
            list.Insert(0, init);

            return AggregateNonEmpty(list, accumulator);
        })
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> Propagate<T>(this T start, Func<T, (T next, bool @continue)> next)
    {
        T current = start;

        yield return current;

        while (next(current) is (T n, true))
            yield return current = n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> Propagate<T>(this T start, Func<T, T> next, Predicate<T> @while)
    {
        T current = start;

        yield return current;

        while (@while(current))
            yield return current = next(current);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Are<T>(this IEnumerable<T> xs, IEnumerable<T> ys, IEqualityComparer<T> comparer) => xs.Are(ys, comparer.Equals);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Are<T>(this IEnumerable<T> xs, IEnumerable<T> ys, Func<T, T, bool> comparer)
    {
        if (xs == ys || (xs?.Equals(ys) ?? ys is null))
            return true;

        using IEnumerator<T> ex = xs.GetEnumerator();
        using IEnumerator<T> ey = ys.GetEnumerator();
        bool cm;

        do
        {
            cm = ex.MoveNext();

            if (cm != ey.MoveNext() || (cm && !comparer(ex.Current, ey.Current)))
                return false;
        }
        while (cm);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Do<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T item in collection)
            action(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Do<T>(this IEnumerable<T> collection, delegate*<T, void> action)
    {
        foreach (T item in collection)
            action(item);
    }

    public static int GetHashCode<T>(IEnumerable<T> values) => GetHashCode(values.ToArray());

    public static int GetHashCode<T>(params T[] arr)
    {
        int hc = arr.Length;

        foreach (T elem in arr)
            hc = HashCode.Combine(hc, elem);

        return hc;
    }
}
