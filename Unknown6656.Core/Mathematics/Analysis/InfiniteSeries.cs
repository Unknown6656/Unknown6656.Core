using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Analysis;


public record InfiniteSeriesSettings<Scalar>(int MaxIterationCount, Scalar Epsilon) where Scalar : IScalar<Scalar>;

public record InfiniteSeriesSettings
    : InfiniteSeriesSettings<Scalar>
{
    public static InfiniteSeriesSettings Default { get; } = new(2000, Scalar.ComputationalEpsilon);

    public InfiniteSeriesSettings(int MaxIterationCount, Scalar Epsilon)
        : base(MaxIterationCount, Epsilon)
    {
    }
}

public static class InfiniteSeries
{
    // TODO

    public static Ring Sin<Ring>(Ring ring, InfiniteSeriesSettings? settings)
        where Ring : IRing<Ring>, Algebra<Scalar>.IVectorSpace<Ring>, Algebra<Scalar>.IMetricVectorSpace
    {
        settings ??= InfiniteSeriesSettings.Default;

        Ring result = ring;
        Ring exponent = ring;
        Ring squared = result.Multiply(ring);
        Ring last = result;
        Scalar factor = Scalar.One;

        for (int i = 1; i < settings.MaxIterationCount; i += 2)
        {
            result = exponent.Multiply(factor).Add(result);
            factor *= -i * (i + 1);
            exponent = exponent.Multiply(squared);

            if (result.Subtract(last).Length < settings.Epsilon)
                break;
            else
                last = result;
        }

        return result;
    }

    public static Ring Cos<Ring>(Ring ring, InfiniteSeriesSettings? settings)
        where Ring : IRing<Ring>, Algebra<Scalar>.IVectorSpace<Ring>, Algebra<Scalar>.IMetricVectorSpace
    {
        settings ??= InfiniteSeriesSettings.Default;

        Ring? result = Ring.One;

        if (result is null)
            throw new ArgumentException($"The type '{typeof(Ring)}' does not define a non-null property '{nameof(Ring.One)}'.", nameof(Ring));

        Ring exponent = ring;
        Ring squared = ring.Multiply(ring);
        Ring last = result;
        Scalar factor = Scalar.One;

        for (int i = 2; i < settings.MaxIterationCount; i += 2)
        {
            factor *= -i * (i - 1);
            result += factor * exponent;
            exponent *= squared;

            if (result.Subtract(last).Length < settings.Epsilon)
                break;
            else
                last = result;
        }

        return result;
    }

    public static Ring Exp<Ring>(Ring ring, InfiniteSeriesSettings? settings)
        where Ring : IRing<Ring>, Algebra<Scalar>.IVectorSpace<Ring>, Algebra<Scalar>.IMetricVectorSpace
    {
        settings ??= InfiniteSeriesSettings.Default;

        Ring? result = Ring.One;

        if (result is null)
            throw new ArgumentException($"The type '{typeof(Ring)}' does not define a non-null property '{nameof(Ring.One)}'.", nameof(Ring));

        Ring exponent = ring;
        Ring last = result;
        Scalar factor = Scalar.One;

        for (int i = 1; i < settings.MaxIterationCount; ++i)
        {
            result += factor.Inverse * exponent;
            factor *= i;
            exponent *= @ring;

            if (result.Subtract(last).Length < settings.Epsilon)
                break;
            else
                last = result;
        }

        return result;
    }
}
