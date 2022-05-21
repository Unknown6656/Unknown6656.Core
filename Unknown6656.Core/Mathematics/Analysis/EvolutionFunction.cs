using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Analysis;


public interface IEvolutionFunction<T>
{
    public int CurrentIteration { get; }
    public T CurrentValue { get; }
    public T PreviousValue { get; }

    public void Reset();
    public void Iterate();
    public void Iterate(int count);
}

public abstract class EvolutionFunction<T>
    : IEvolutionFunction<T?>
    where T : IGroup<T>
{
    private protected readonly List<T?> _values = new();
    private T? _initial_val = default;

    public T? InitialValue
    {
        get => _initial_val;
        set => UpdateInitialValue(value);
    }

    public IEnumerable<T?> PastValues => _values;
    public int CurrentIteration => _values.Count - 1;
    public T? CurrentValue => _values[CurrentIteration];
    public T? PreviousValue => _values[CurrentIteration - 1];
    public T CurrentVelocity => CurrentValue! - PreviousValue!;
    public T PreviousVelocity => PreviousValue! - _values[CurrentIteration - 2]!;
    public T CurrentAcceleration => CurrentVelocity - PreviousVelocity;


    public virtual void Reset()
    {
        _values.Clear();
        _values.Add(InitialValue);
    }

    public void Iterate(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        while (count --> 0)
            Iterate();
    }

    public virtual void Iterate()
    {
        if (_values.Count is 0)
            _values.Add(InitialValue);

        _values.Add(ComputeNextValue());
    }

    protected abstract T? ComputeNextValue();

    public void UpdateInitialValue(T? new_initial_value)
    {
        if (_initial_val?.IsNot(new_initial_value) ?? new_initial_value?.IsNot(_initial_val) ?? true)
        {
            _initial_val = new_initial_value;

            int count = CurrentIteration + 1;

            Reset();

            while (count-- > 0)
                Iterate();
        }
    }

    public static EvolutionFunction<T> FromDelegate(Func<T?, T?> function, T? initial = default) => FromDelegate((v, _) => function(v), initial);

    public static EvolutionFunction<T> FromDelegate(Func<T?, EvolutionFunction<T>, T?> function, T? initial = default) => new _delegated(function, initial);


    private sealed class _delegated
        : EvolutionFunction<T>
    {
        private readonly Func<T?, EvolutionFunction<T>, T?> _func;


        public _delegated(Func<T?, EvolutionFunction<T>, T?> func, T? initial)
        {
            InitialValue = initial;
            _func = func;
        }

        protected override T? ComputeNextValue() => _func(CurrentValue, this);
    }
}

public class MultiPointEvolutionFunction<Function, T>
    : IEvolutionFunction<T?[]>
    where Function : EvolutionFunction<T>
    where T : IGroup<T>
{
    private readonly Func<Function> _constructor;


    public Function[] Evolutions { get; private set; }

    public int CurrentIteration => Evolutions.FirstOrDefault()?.CurrentIteration ?? 0;

    public T?[] CurrentValues => Evolutions.ToArray(f => f.CurrentValue);

    public T?[] PreviousValues => Evolutions.ToArray(f => f.PreviousValue);

    public T[] CurrentVelocities => Evolutions.ToArray(f => f.CurrentVelocity);

    public T[] PreviousVelocities => Evolutions.ToArray(f => f.PreviousVelocity);

    public T[] CurrentAccelerations => Evolutions.ToArray(f => f.CurrentAcceleration);

    T?[] IEvolutionFunction<T?[]>.CurrentValue => CurrentValues;

    T?[] IEvolutionFunction<T?[]>.PreviousValue => PreviousValues;


    public MultiPointEvolutionFunction(Func<Function> constructor, params T[] initial_values)
        : this(constructor, initial_values as IEnumerable<T>)
    {
    }

    public MultiPointEvolutionFunction(params T[] initial_values)
        : this(initial_values as IEnumerable<T>)
    {
    }

    public MultiPointEvolutionFunction(IEnumerable<T> initial_values)
        : this(Activator.CreateInstance<Function>, initial_values)
    {
    }

    public MultiPointEvolutionFunction(Func<Function> constructor, IEnumerable<T> initial_values)
    {
        _constructor = constructor;
        Evolutions = initial_values.ToArray(initial =>
        {
            Function func = constructor();

            func.InitialValue = initial;

            return func;
        });
    }

    public void UpdateInitialValues(IEnumerable<T> initial_values)
    {
        T[] arr = initial_values.ToArray();
        int old_sz = Evolutions.Length;

        if (arr.Length != Evolutions.Length)
        {
            Function[] evos = Evolutions;

            Array.Resize(ref evos, arr.Length);

            Evolutions = evos;
        }

        for (int i = 0; i < arr.Length; ++i)
            if (i < old_sz)
                Evolutions[i].UpdateInitialValue(arr[i]);
            else
            {
                Evolutions[i] = _constructor();
                Evolutions[i].InitialValue = arr[i];
                Evolutions[i].Iterate(CurrentIteration);
            }
    }

    public void Iterate(int count) => Evolutions.Do(f => f.Iterate(count));

    public void Iterate() => Iterate(1);

    public void Reset() => Evolutions.Do(f => f.Reset());
}

public class EvolutionFunction2D
    : EvolutionFunction<Vector2>
{
    private readonly Func<Vector2, EvolutionFunction2D, Vector2> _iterator;


    public EvolutionFunction2D(Func<Vector2, Vector2> iterator)
        : this((v, _) => iterator(v))
    {
    }

    public EvolutionFunction2D(Func<Vector2, EvolutionFunction2D, Vector2> iterator) => _iterator = iterator;

    public EvolutionFunction2D(Function<Vector2, Vector2> iterator)
        : this(iterator.Evaluate)
    {
    }

    protected override Vector2 ComputeNextValue() => _iterator(CurrentValue, this);


    public static EvolutionFunction2D GingerBreadMap => new(v => new(1 - v.Y + v.X.Abs(), v.X));

    public static EvolutionFunction2D ExponentialMap(Vector2 c) => new(v => v.ToComplex().Exp().Add(c).ToVector());

    public static EvolutionFunction2D MandelbrotMap(Vector2 c, int exponent = 2) => new(v => v.ToComplex().Power(exponent).Add(c).ToVector());

    public static EvolutionFunction2D GaussMap(Scalar a, Scalar b) => new(v => v.ToComplex().Power(2).Multiply(-a).Exp().Add(b));

    public static EvolutionFunction2D HenanAttractorMap(Scalar a, Scalar b) => new(v => new(1 - a * v.X * v.X + v.Y, b * v.X));

    public static EvolutionFunction2D DuffingMap(Scalar a, Scalar b) => new(v => new(v.Y, -b * v.X + a * v.Y - v.Y.Power(3)));
}
