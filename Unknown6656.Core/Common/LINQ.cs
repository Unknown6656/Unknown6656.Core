using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.Mathematics.Numerics;
using Unknown6656.IO;

using Random = Unknown6656.Mathematics.Numerics.Random;

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
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection) => collection.Shuffle(new XorShift());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Shuffle<T>(this IEnumerable<T> collection, Random rng)
    {
        T[] array = collection.ToArray();
        int n = array.Length;

        while (n --> 0)
        {
            int k = rng.NextInt(n + 1);

            (array[k], array[n]) = (array[n], array[k]);
        }

        return array;
    }
}
