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
}
