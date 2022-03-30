﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;
using System.Runtime.CompilerServices;
using Unknown6656.Imaging.Effects;

namespace Unknown6656.Mathematics.Analysis;


/// <completionlist cref="AnimationFunction"/>
public class AnimationFunction
    : ScalarFunction
{
    /// <summary>
    /// Returns a function which step-wise interpolates between two values. The amount of steps is specified passing it as an <see cref="int"/>
    /// to the indexer of this property. The minimum step count must be two.
    /// <para/>
    /// <code>
    ///   to + - - -┌─
    ///      +    ┌─┘'
    ///      +  ┌─┘  '
    /// from + ─┘    '
    ///      '─+─+─+─+──>
    ///        0     1
    /// </code>
    /// </summary>
    public static ReadOnlyIndexer<int, AnimationFunction> Stepwise { get; } = new(steps =>
        steps < 2 ? throw new ArgumentOutOfRangeException(nameof(steps)) : (new(x => (steps * x).Floor.Multiply(x - 1), true)));

    /// <summary>
    /// Returns the discrete transition function which switches at x=<c>0.5</c> between the start and end value.
    /// </summary>
    public static AnimationFunction Discrete { get; } = Stepwise[2];

    /// <summary>
    /// Represents the following interpolation function:
    /// <para/>
    /// <code>
    /// f(x) = x
    /// </code>
    /// </summary>
    public static AnimationFunction Linear { get; } = new(LINQ.id, true);

    /// <summary>
    /// Represents the following interpolation function:
    /// <para/>
    /// <code>
    /// f(x) = 3x² + 2x³
    /// </code>
    /// </summary>
    public static AnimationFunction Smoothstep { get; } = new(x => x.Multiply(x, 3).Subtract(x.Multiply(x, x, 2)));

    /// <summary>
    /// Represents the following interpolation function:
    /// <para/>
    /// <code>
    /// f(x) = 10x³ - 15x⁴ + 6x⁵
    /// </code>
    /// </summary>
    public static AnimationFunction Smootherstep { get; } = new(new Polynomial(0, 0, 0, 10, -15, 6)); // 10x^3 - 15x^4 + 6x^5

    public static AnimationFunction InverseSmoothstep { get; } = new(x => .5 - Math.Sin(Math.Asin(1 - 2 * x) * .333333333333333333333333333333333333333333333));

    /// <summary>
    /// Represents the generalized smoothstep function generated from the given degree N.
    /// <para/>
    /// See <a href="https://en.wikipedia.org/wiki/Smoothstep">https://en.wikipedia.org/wiki/Smoothstep</a>.
    /// </summary>
    public static ReadOnlyIndexer<int, AnimationFunction> GeneralSmoothstep { get; } = new(N => new(x =>
    {
        Scalar result = 0;

        for (int n = 0; n <= N; ++n)
            result += MathExtensions.BinomialCoefficient(-N - 1, n) *
                      MathExtensions.BinomialCoefficient(2 * N + 1, N - n) *
                      Math.Pow(x, N + n + 1);

        return result;
    }));

    public bool IsExtrapolationEnabled { get; }


    public AnimationFunction(Func<Scalar, Scalar> func, bool enable_extrapolation = false)
        : base(enable_extrapolation ? func : (x => x < 0 ? 0 : x > 1 ? 1 : func(x))) => IsExtrapolationEnabled = enable_extrapolation;

    public T Interpolate<T>(T from, T to, Scalar x)
        where T : Algebra<Scalar>.IVectorSpace<T> => from.LinearInterpolate(in to, x);

    public void Animate<T>(T from, T to, Action<T> callback, AnimationConfiguration config)
        where T : Algebra<Scalar>.IVectorSpace<T> => AnimateAsync(from, to, callback, config).GetAwaiter().GetResult();

    public void Animate<T>(T from, T to, Func<T, Task> callback, AnimationConfiguration config)
        where T : Algebra<Scalar>.IVectorSpace<T> => AnimateAsync(from, to, callback, config).GetAwaiter().GetResult();

    public async Task AnimateAsync<T>(T from, T to, Action<T> callback, AnimationConfiguration config)
        where T : Algebra<Scalar>.IVectorSpace<T> => await AnimateAsync(from, to, t => { callback(t); return Task.CompletedTask; }, config);

    public async Task AnimateAsync<T>(T from, T to, Func<T, Task> callback, AnimationConfiguration config)
        where T : Algebra<Scalar>.IVectorSpace<T>
    {
        long total = config.Duration.Ticks;
        long step = Math.Min(config.SteppingInterval.Ticks, total);

        if (step < 0)
            throw new ArgumentException("Invalid stepping interval.", nameof(config));

        Stopwatch elapsed = Stopwatch.StartNew();

        await callback(from);

        while (elapsed.ElapsedTicks is long e && e < total)
        {
            await callback(Interpolate(from, to, e / (double)total));
            await Task.Delay(step);
        }

        await callback(to);
    }

    public static explicit operator AnimationFunction(Polynomial pol) => new(pol);
}

public record AnimationConfiguration(TimeSpan Duration, TimeSpan SteppingInterval)
{
    public AnimationConfiguration(TimeSpan duration, long steps)
        : this(duration, steps > 0 ? new TimeSpan(duration.Ticks / steps) : throw new ArgumentOutOfRangeException(nameof(steps)))
    {
    }
}
