using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Mathematics.Numerics;


public static class Sequences
{
    public static Sequence<bint> AllIntegers => (0, i => i > 0 ? -i : 1 - i);

    public static Sequence<bint> PoitiveIntegers => (0, i => i + 1);

    public static Sequence<bint> NegativeIntegers => (0, i => i - 1);

    public static Sequence<int> Primes => new(MathExtensions._primes);

    public static Sequence<bint> Collatz(bint start = default) => (start, i => i.IsEven ? i / 2 : i * 3 + 1);

    public static Sequence<double> AllNumbers(double epsilon = double.Epsilon * 2) => (0, s => -s * (1 + (s > 0 ? 0 : epsilon)));

    public static Sequence<Scalar> InclusiveRange(Scalar start, Scalar end, uint stepcount) => InclusiveInterval(start, end, (end - start) / stepcount);

    public static Sequence<Scalar> InclusiveInterval(Scalar start, Scalar end) => InclusiveInterval(start, end, Scalar.ComputationalEpsilon);

    public static Sequence<Scalar> InclusiveInterval(Scalar start, Scalar end, Scalar step)
    {
        IEnumerable<Scalar> iter()
        {
            yield return start;

            if (!start.Is(end))
            {
                bool asc = start < end;

                if (asc && step <= 0)
                    throw new ArgumentException("The step size must be greater than zero.", nameof(step));
                else if (!asc && step >= 0)
                    throw new ArgumentException("The step size must be smaller than zero.", nameof(step));

                while (!start.Is(end))
                    yield return asc ? start += step : start -= step;
            }
        }

        return iter().ToSequence();
    }

    public static Sequence<Scalar> OpenInterval(Scalar start, bool ascending = true) => OpenInterval(start, Scalar.ComputationalEpsilon, ascending);

    public static Sequence<Scalar> OpenInterval(Scalar start, Scalar step, bool ascending = true) => (start, i => ascending ? i + step : i - step);


    public static Sequence<T> ToSequence<T>(this IEnumerable<T> coll) => new(coll);
}

public class Sequence<T>
    : IEnumerable<T>
{
    private readonly Func<IEnumerable<T>> _iterator;

    public IEnumerable<T> Items => _iterator();

    public IEnumerable<(int index, T item)> IndexedItems => Items.Zip(Sequences.PoitiveIntegers).Select(t => ((int)t.Second, t.First));

    public T this[int index] => (index switch
    {
        0 => Items,
        > 0 => Items.Skip(index - 1),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    }).First();


    public Sequence(IEnumerable<T> enumerable)
        : this(() => enumerable)
    {
    }

    public Sequence(Func<IEnumerable<T>> iterator) => _iterator = iterator;

    public Sequence(T seed, Func<T, T> next)
        : this(CreateEnumerable(seed, next))
    {
    }

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => (Items as IEnumerable).GetEnumerator();

    private static IEnumerable<T> CreateEnumerable(T seed, Func<T, T> next)
    {
        while (true)
        {
            yield return seed;

            seed = next(seed);
        }
    }


    public static implicit operator Sequence<T>(Func<IEnumerable<T>> f) => new(f);

    public static implicit operator Sequence<T>((T seed, Func<T, T> next) f) => new(f.seed, f.next);
}
