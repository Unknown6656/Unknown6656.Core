using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

using Unknown6656.Mathematics.Analysis;
using Unknown6656.IO;

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
}
